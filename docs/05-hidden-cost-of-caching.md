# The hidden cost of caching: detecting stale reads with locust

## Stale reads are real

Update a record. Refresh the page. Still see the old value? That’s not just annoying - it’s a sign your cache lied to you.

### What is a stale cache?

A **stale cache** returns outdated data after the underlying source has changed. It’s a silent failure mode.

Common causes:

- **race conditions**: a write completes after a read pulls from the cache
- **delayed expiration**: time-based invalidation misses fast updates
- **distributed lag**: not all nodes get the update simultaneously

### Why it matters

Stale data isn’t just a UX issue. It’s a logic bomb:

- Decisions based on wrong values
- Reports with ghost records
- Users distrusting the system

Most systems tolerate a bit of staleness. But if you want to **see** it, you need stress.

## How to detect stale caches

We built a simple visibility mechanism:

1. **simulate load**: call the same book record with concurrent updates and reads
2. **track updates**: embed a counter in the book’s title
3. **detect mismatch**: if a read returns an older counter than the latest write → stale cache

```text
✅ match → consistent
❌ lower counter → stale
```

> ⚠️ This technique only works reliably in **single-process tests**. Distributed workers don’t share state, so each gets its own version of the counter.

## Load testing with locust

### Why load testing matters

Most bugs don’t show up during manual tests. Code looks clean. But when traffic spikes, timing breaks things. Load testing reveals what unit tests can’t.

This is how a stale cache may happen:

![Stale Caching]

### Why we use locust

[Locust] is a Python-based, event-driven load testing tool. It gives us:

- async I/O for high scale
- custom user behavior
- live UI with stats and failures
- active community

### Project structure

We organize tests like this:

```
📂 tests/python/load-tests/
 ├── common/
 │   ├── api.py                 # API client helpers
 │   ├── config.py              # base URL config
 ├── locustfiles/
 │   ├── stale_cache.py         # basic stale cache test
 │   ├── stale_cache_wave.py    # wave simulation
 ├── locust.conf                # locust settings
```

## Running the test: setup

We use `uv` for speed. Here’s the setup:

```bash
cd tests/python/load-tests
uv venv
uv pip install -r requirements.txt
```

> The `requirements.txt` pins the Locust version.

### Locust config: `locust.conf`

```ini
host = http://localhost:5000
users = 90
spawn-rate = 5
run-time = 30s
```

- 90 users
- 5 new users per second
- 30 seconds test duration

## Lifecycle: setup and cleanup

Each test run sets up fresh data and deletes it afterward. Here’s how:

```python
import requests
import common.config as config

from locust import events
from locust.runners import WorkerRunner
from common.api import Api

api = Api(requests, f"{config.API_URL}")

@events.test_start.add_listener
def on_test_start(environment, **kwargs):
    if isinstance(environment.runner, WorkerRunner):
        return

    payload = {
        "title": "StaleCache Book 1",
        "author": "StaleCacheUser",
        "isbn": "1234567890123",
    }
    try:
        api.add_book(payload)
    except Exception as e:
        print(f"[SETUP] Failed to add book: {e}")

@events.test_stop.add_listener
def on_test_stop(environment, **kwargs):
    if isinstance(environment.runner, WorkerRunner):
        return

    try:
        books = api.get_all_books()
        for book in books:
            api.delete_book(book.get("id"))
    except Exception as e:
        print(f"[CLEANUP] Failed to delete books: {e}")
```

- `test_start` adds a test record
- `test_stop` deletes all
- Guards skip this logic for distributed workers

## Detecting stale reads: core test logic

Here’s the main test in `stale_cache.py`:

```python
import time
import threading
import events_handler

from locust import FastHttpUser, task, between
from common.api import Api

global_counter = 0
counter_lock = threading.Lock()

class StaleCacheUser(FastHttpUser):
    wait_time = between(0.01, 0.02)

    def on_start(self):
        self.api = Api(self.client, "/books")
        self.books = self.api.get_all_books()

    @task
    def stale_cache(self):
        if not self.books:
            return

        book_id = self.books[0]["id"]
        user_id = self.environment.runner.user_count

        global global_counter
        with counter_lock:
            global_counter += 1
            my_counter = global_counter

        title = f"user:{user_id}-title-{my_counter}"
        payload = {"title": title, "author": "StaleCache", "isbn": "1234567890123"}
        self.api.update_book(book_id, payload)
        time.sleep(0.05)

        data = self.api.get_book(book_id)
        fetched_title = data.get("title", "")
        fetched_counter = int(fetched_title.split("-")[-1])

        if fetched_counter < my_counter:
            self.environment.runner.stats.log_error(
                "GET",
                f"/books/{book_id}",
                f"⚠️ Stale cache detected! Wrote '{title}', but read '{fetched_title}'",
            )
```

- Counter ensures we track each write
- Lock avoids race conditions across threads
- Reads are validated immediately after writes

### Running the test

```bash
locust -f locustfiles/stale_cache.py
```

Open your browser at [http://localhost:8089](http://localhost:8089) to monitor in real time.

Check the **Failures tab**. Each failure means a stale read: the written title had a higher counter than the read.

![Locust stale cache load test]

## Wave simulation: more realistic traffic

Users don’t come all at once. They arrive in bursts - morning rush, lunch spike, evening surge.

We simulate this using `LoadTestShape`:

```python
from stale_cache import StaleCacheUser
from locust import LoadTestShape


class WaveShape(LoadTestShape):
    wave_users = [3, 30, 3, 60, 3, 90, 3]  # Users per wave
    wave_duration = 20  # seconds per wave

    def tick(self):
        run_time = self.get_run_time()
        wave = int(run_time // self.wave_duration)
        if wave < len(self.wave_users):
            users = self.wave_users[wave]
            print(f"[WaveShape] At {run_time:.1f}s: wave {wave}, users={users}")
            return (users, users)  # (user_count, spawn_rate)
        print(f"[WaveShape] Test finished at {run_time:.1f}s")
        return None

```

### Run it

```bash
locust -f locustfiles/stale_cache_wave.py
```

Watch the wave pattern in the **Failures tab** - spikes followed by calm.

![Locust stale cache wave load test]

## Should we fix stale caches?

### Maybe not.

Some systems **need** consistency. For others, eventual is good enough.

- Fix it if: your app needs real-time accuracy (banking, auctions, dashboards).
- Leave it if: your app can survive a short delay (feeds, comments, inventory views).

Choose based on what breaks when data is stale.

## What we learned

- 🧠 stale reads happen under pressure
- 🔍 locust helps surface silent failures
- 🌊 real users behave in waves
- ⚖️ consistency isn’t free - know when to pay

**source code**: [GitHub repository]

[Architecture Load Testing]: ../../.assets/architecture-load-testing.svg
[Locust]: https://locust.io
[Stale Caching]: ../.assets/stale-caching.svg
[Locust stale cache load test]: ../.assets/stale-cache-load-test.gif
[Locust stale cache load test failures]: ../.assets/stale-cache-load-test-failures.png
[Locust stale cache wave load test]: ../.assets/stale-cache-wave-load-test.gif
[GitHub repository]: https://github.com/dorinandreidragan/books-inventory/tree/episode/05-hidden-cost-of-caching

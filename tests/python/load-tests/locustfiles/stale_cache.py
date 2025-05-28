import requests
import time
import threading
import common.config as config

from locust import FastHttpUser, task, between, events
from locust.runners import WorkerRunner
from common.api import Api

global_seq = 0
seq_lock = threading.Lock()
book_ids = []


@events.test_start.add_listener
def on_test_start(environment, **kwargs):
    if isinstance(environment.runner, WorkerRunner):
        return
    global book_ids
    book_ids.clear()
    for i in range(1):
        payload = {
            "title": f"StaleCache Book {i+1}",
            "author": "StaleCacheUser",
            "isbn": "1234567890123",
        }
        try:
            resp = requests.post(f"{config.API_URL}", json=payload)
            resp.raise_for_status()
            data = resp.json()
            book_id = data.get("id")
            if book_id is not None:
                book_ids.append(book_id)
        except Exception as e:
            print(f"[SETUP] Failed to add book {payload['title']}: {e}")


@events.test_stop.add_listener
def on_test_stop(environment, **kwargs):
    if isinstance(environment.runner, WorkerRunner):
        return
    global book_ids
    for book_id in book_ids:
        try:
            resp = requests.delete(f"{config.API_URL}/{book_id}")
            resp.raise_for_status()
        except Exception as e:
            print(f"[CLEANUP] Exception deleting book {book_id}: {e}")
    book_ids.clear()


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

        # Increment seq
        global global_seq
        with seq_lock:
            global_seq += 1
            my_seq = global_seq

        # Write
        my_title = f"user:{user_id}-title-{my_seq}"
        payload = {"title": my_title, "author": "StaleCache", "isbn": "1234567890123"}
        self.api.update_book(book_id, payload)
        time.sleep(0.05)

        # Read immediately
        data = self.api.get_book(book_id)
        fetched_title = data.get("title", "")

        # Parse sequence
        fetched_seq = int(fetched_title.split("-")[-1])

        # Log stale cache
        if fetched_seq < my_seq:
            self.environment.runner.stats.log_error(
                "GET",
                f"/books/{book_id}",
                f"⚠️ Stale cache detected! Updated title: '{my_title}', but read '{fetched_title}' (fetched_seq: {fetched_seq} < my_seq: {my_seq})",
            )

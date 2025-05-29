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

        # Increment counter
        global global_counter
        with counter_lock:
            global_counter += 1
            my_counter = global_counter

        # Write
        my_title = f"user:{user_id}-title-{my_counter}"
        payload = {"title": my_title, "author": "StaleCache", "isbn": "1234567890123"}
        self.api.update_book(book_id, payload)
        time.sleep(0.05)

        # Read immediately
        data = self.api.get_book(book_id)
        fetched_title = data.get("title", "")

        # Parse counter
        fetched_counter = int(fetched_title.split("-")[-1])

        # Log stale cache
        if fetched_counter < my_counter:
            self.environment.runner.stats.log_error(
                "GET",
                f"/books/{book_id}",
                f"⚠️ Stale cache detected! Updated title: '{my_title}', but read '{fetched_title}' (my_counter: {my_counter} < fetched_counter: {fetched_counter})",
            )

"""
Locust test suite to spot race conditions when updating a book in Books Inventory API.
This suite simulates concurrent updates to the same book records to reveal lost updates or data corruption.
"""
import random
import time
import logging
from locust import HttpUser, task, between, events


BOOK_TITLES = [f"Race Book {i}" for i in range(1, 6)]
BOOK_IDS = []  # Will be populated with real IDs from API responses

class BookRaceConditionUser(HttpUser):
    wait_time = between(0.05, 0.2)


    def on_start(self):
        # Add books and collect their real IDs from API responses
        global BOOK_IDS
        BOOK_IDS.clear()
        unique_run_id = f"{int(time.time() * 1000)}-{random.randint(1000,9999)}"
        for i, title in enumerate(BOOK_TITLES, 1):
            unique_title = f"{title} [{unique_run_id}]"
            unique_isbn = f"RC-{i}-{unique_run_id}"
            resp = self.client.post("/addBook", json={
                "title": unique_title,
                "author": "Race Tester",
                "isbn": unique_isbn
            })
            if resp.status_code == 200:
                try:
                    data = resp.json()
                    book_id = data.get("bookId")
                    if book_id is not None:
                        BOOK_IDS.append(book_id)
                except Exception as e:
                    logging.error(f"Failed to parse addBook response: {e}")
            else:
                logging.error(f"Failed to add book {unique_title}: {resp.status_code} - {resp.text}")

    @task(4)
    def update_with_read(self):
        """Read a book, wait, then update it (classic lost update scenario)."""
        if not BOOK_IDS:
            logging.warning("No book IDs available for update_with_read task.")
            return
        book_id = random.choice(BOOK_IDS)
        resp = self.client.get(f"/books/{book_id}")
        if resp.status_code == 200:
            book = resp.json()
            # Simulate user think time
            time.sleep(random.uniform(0.05, 0.2))
            # Change the title to a unique value
            new_title = f"{book['title']} [upd-{random.randint(1000,9999)}]"
            update_payload = {
                "title": new_title,
                "author": book["author"],
                "isbn": book["isbn"]
            }
            put_resp = self.client.put(f"/books/{book_id}", json=update_payload)
            if put_resp.status_code != 200:
                logging.warning(f"Race condition suspected: PUT {book_id} failed with {put_resp.status_code}")

    @task(2)
    def blind_update(self):
        """Update a book without reading it first (maximizes concurrent writes)."""
        if not BOOK_IDS:
            logging.warning("No book IDs available for blind_update task.")
            return
        book_id = random.choice(BOOK_IDS)
        update_payload = {
            "title": f"Blind Update {random.randint(10000,99999)}",
            "author": "Race Tester",
            "isbn": f"RC-{book_id}"
        }
        resp = self.client.put(f"/books/{book_id}", json=update_payload)
        if resp.status_code != 200:
            logging.warning(f"Blind update failed: PUT {book_id} {resp.status_code}")

# Custom event: log failed PUTs as possible race conditions
def log_race_condition(request_type, name, response_time, response_length, response, exception, **kwargs):
    if request_type == "PUT" and response is not None and response.status_code >= 409:
        logging.error(f"Race condition detected: {name} {response.status_code}")
        events.request_failure.fire(
            request_type=request_type,
            name=name + "_race_condition",
            response_time=response_time,
            exception=exception or Exception(f"Race condition: {response.status_code}")
        )

# Register the event listener
@events.request.add_listener
def on_request(request_type, name, response_time, response_length, response, exception, **kwargs):
    log_race_condition(request_type, name, response_time, response_length, response, exception, **kwargs)

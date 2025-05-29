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

    for i in range(1):
        payload = {
            "title": f"StaleCache Book {i+1}",
            "author": "StaleCacheUser",
            "isbn": "1234567890123",
        }
        try:
            api.add_book(payload)
        except Exception as e:
            print(f"[SETUP] Failed to add book {payload['title']}: {e}")


@events.test_stop.add_listener
def on_test_stop(environment, **kwargs):
    if isinstance(environment.runner, WorkerRunner):
        return

    try:
        books = api.get_all_books()
        for book in books:
            book_id = book.get("id")
            api.delete_book(book_id)
    except Exception as e:
        print(f"[CLEANUP] Exception deleting all books: {e}")

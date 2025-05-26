from locust import HttpUser, task, between, events
import random
import time
import logging

class RaceConditionUser(HttpUser):
    wait_time = between(0.1, 0.5)  # Short wait times to increase concurrency
    
    def on_start(self):
        """Add some books to use in our tests."""
        # Add a few books that we'll use for race condition testing
        for i in range(5):
            self.client.post("/addBook", json={
                "Title": f"Race Condition Test Book {i}",
                "Author": "Test Author",
                "ISBN": f"12345_{i}"
            })
    
    @task(3)
    def read_then_update_book(self):
        """Simulate a race condition by reading then updating the same book."""
        # Use a small set of book IDs to increase contention
        book_id = random.randint(1, 5)
        
        # GET the book
        response = self.client.get(f"/books/{book_id}")
        if response.status_code == 200:
            book = response.json()
            
            # Simulate some processing time
            time.sleep(0.1)
            
            # Update the book with new data
            # This is where race conditions can occur if another user has updated
            # the book between our GET and PUT
            updated_book = {
                "title": book["title"] + " - Updated",
                "author": book["author"],
                "isbn": book["isbn"]
            }
            
            put_response = self.client.put(f"/books/{book_id}", json=updated_book)
            if put_response.status_code != 200:
                logging.info(f"Potential race condition detected: PUT failed with status {put_response.status_code}")
    
    @task(2)
    def concurrent_updates(self):
        """Directly update a book without reading first."""
        book_id = random.randint(1, 5)
        
        # Update with random data to increase chance of conflicts
        updated_book = {
            "title": f"Concurrent Update {random.randint(1000, 9999)}",
            "author": "Test Author",
            "isbn": f"12345_{book_id}"
        }
        
        response = self.client.put(f"/books/{book_id}", json=updated_book)
        if response.status_code != 200:
            logging.info(f"Update conflict detected: Status {response.status_code}")

# Track statistics for race conditions
@events.request.add_listener
def on_request(request_type, name, response_time, response_length, exception, **kwargs):
    if request_type == "PUT" and kwargs.get("response", None) and kwargs["response"].status_code >= 400:
        events.request_failure.fire(
            request_type=request_type,
            name=name + "_race_condition",
            response_time=response_time,
            exception=exception
        )
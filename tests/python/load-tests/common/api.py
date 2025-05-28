class Api:
    def __init__(self, client, url):
        self.client = client
        self.api_url = url

    def add_book(self, payload):
        resp = self.client.post(self.api_url, json=payload)
        resp.raise_for_status()
        return resp.json()

    def update_book(self, book_id, payload):
        resp = self.client.put(f"{self.api_url}/{book_id}", json=payload)
        resp.raise_for_status()
        return resp.json() if resp.content else None

    def get_book(self, book_id):
        resp = self.client.get(f"{self.api_url}/{book_id}")
        resp.raise_for_status()
        return resp.json()

    def get_all_books(self):
        resp = self.client.get(f"{self.api_url}")
        resp.raise_for_status()
        return resp.json()

    def delete_book(self, book_id):
        resp = self.client.delete(f"{self.api_url}/{book_id}")
        resp.raise_for_status()
        return resp.status_code

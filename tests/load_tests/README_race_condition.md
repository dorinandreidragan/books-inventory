# Locust Race Condition Test Suite

This test suite (`locust_race_condition.py`) is designed to spot race conditions when updating books in the Books Inventory API.

## How It Works

- Simulates multiple users concurrently reading and updating the same set of books.
- Two main tasks:
  - **update_with_read**: Reads a book, waits, then updates it (classic lost update scenario).
  - **blind_update**: Updates a book without reading it first (maximizes concurrent writes).
- Logs failed or conflicting updates as potential race conditions.

## Usage

1. Ensure the API is running and accessible.
2. From the `tests/load_tests/` directory, run:

```zsh
locust -f locust_race_condition.py
```

3. Open the Locust web UI (default: http://localhost:8089), set the number of users and spawn rate, and start the test.

## Interpreting Results

- Look for failed or 409 (Conflict) responses in the Locust UI and logs.
- High rates of failed/conflicting updates may indicate race conditions or lack of concurrency control in the API.

## Requirements

- Python 3.10+
- Locust (install via `uv pip install locust` or see `pyproject.toml`)

---

For more details, see the main `README.md` and API documentation.

from stale_cache import StaleCacheUser
from locust import LoadTestShape


class WaveShape(LoadTestShape):
    """
    A shape that simulates waves of users.
    Each wave lasts for wave_duration seconds and has a different user count.
    """

    wave_users = [5, 20, 5, 50, 5, 80, 5]  # Users per wave
    wave_duration = 10  # seconds per wave

    def tick(self):
        run_time = self.get_run_time()
        wave = int(run_time // self.wave_duration)
        if wave < len(self.wave_users):
            users = self.wave_users[wave]
            print(f"[WaveShape] At {run_time:.1f}s: wave {wave}, users={users}")
            return (users, users)  # (user_count, spawn_rate)
        print(f"[WaveShape] Test finished at {run_time:.1f}s")
        return None

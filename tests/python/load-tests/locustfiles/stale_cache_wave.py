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

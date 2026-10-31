"""Standard-library smoke test. Run after `docker compose up -d --build`.

Usage: python scripts/smoke_test.py
Uses env ADMIN_EMAIL and ADMIN_PASSWORD, never prints the password.
"""
from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request
from http.cookiejar import CookieJar

BASE = os.environ.get("SKIRESORT_URL", "http://localhost:8088").rstrip("/")
jar = CookieJar()
client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))


def call(method: str, path: str, body: object | None = None, csrf: str = "") -> tuple[int, dict]:
    headers = {"Accept": "application/json"}
    if csrf:
        headers["X-CSRF-TOKEN"] = csrf
    data = None
    if body is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(BASE + path, data=data, method=method, headers=headers)
    try:
        with client.open(req, timeout=15) as response:
            return response.status, json.loads(response.read())
    except urllib.error.HTTPError as exc:
        try:
            return exc.code, json.loads(exc.read())
        except ValueError:
            return exc.code, {}


def main() -> None:
    email, password = os.environ.get("ADMIN_EMAIL"), os.environ.get("ADMIN_PASSWORD")
    if not email or not password:
        sys.exit("ADMIN_EMAIL and ADMIN_PASSWORD environment variables required for smoke test.")
    status, payload = call("GET", "/api/health")
    assert status == 200 and payload["status"] == "ok", (status, payload)
    print("[PASS] Public health route")
    status, _ = call("GET", "/api/dashboard")
    assert status == 401, status
    print("[PASS] Anonymous dashboard access denied")
    status, payload = call("GET", "/api/auth/csrf")
    assert status == 200, (status, payload)
    token = payload["token"]
    status, _ = call("POST", "/api/auth/login", {"email": email, "password": password})
    assert status == 400, status
    print("[PASS] CSRF protection enforced")
    status, payload = call("POST", "/api/auth/login", {"email": email, "password": password}, token)
    assert status == 200, (status, payload)
    print("[PASS] Admin login")
    status, payload = call("GET", "/api/dashboard")
    assert status == 200 and "equipmentCount" in payload, (status, payload)
    print("[PASS] Protected dashboard")
    for section in ("customers", "equipment", "rentals", "rooms", "stays", "slopes", "activities", "payments", "employees"):
        status, payload = call("GET", f"/api/{section}")
        assert status == 200 and isinstance(payload, list), (section, status, payload)
        print(f"[PASS] {section} list")
    print("All smoke checks passed.")


if __name__ == "__main__":
    main()

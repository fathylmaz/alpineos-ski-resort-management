"""State-changing integration scenarios. RUN ONLY AGAINST A DISPOSABLE DEMO DB!

Requires running Docker Compose instance; ADMIN_EMAIL/ADMIN_PASSWORD in env.
Records created by this script remain in the database.
"""
from __future__ import annotations

import json
import os
import uuid
import urllib.error
import urllib.request
from datetime import datetime, timedelta, timezone
from http.cookiejar import CookieJar

BASE = os.getenv("SKIRESORT_URL", "http://localhost:8088").rstrip("/")
client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(CookieJar()))


def send(method: str, path: str, body=None, token: str = ""):
    headers = {"Accept": "application/json"}
    if token:
        headers["X-CSRF-TOKEN"] = token
    if body is not None:
        headers["Content-Type"] = "application/json"
    request = urllib.request.Request(
        BASE + "/api" + path,
        json.dumps(body).encode() if body is not None else None,
        headers=headers,
        method=method,
    )
    try:
        with client.open(request, timeout=30) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as exc:
        return exc.code, json.loads(exc.read())


def check(method, path, body=None, expected=200, token=""):
    status, obj = send(method, path, body, token)
    assert status == expected, (method, path, status, expected, obj)
    return obj


def main():
    assert os.getenv("I_CONFIRM_DISPOSABLE_DB") == "YES", "Set I_CONFIRM_DISPOSABLE_DB=YES first; records are written."
    token = check("GET", "/auth/csrf")["token"]
    check("POST", "/auth/login", {"email": os.environ["ADMIN_EMAIL"], "password": os.environ["ADMIN_PASSWORD"]}, token=token)
    token = check("GET", "/auth/csrf")["token"]
    suffix = uuid.uuid4().hex[:10]
    c1 = check("POST", "/customers", {"fullName": "Demo Test A", "email": f"test-a-{suffix}@example.test", "phone": "05000000000"}, 201, token)["id"]
    c2 = check("POST", "/customers", {"fullName": "Demo Test B", "email": f"test-b-{suffix}@example.test", "phone": "05000000000"}, 201, token)["id"]
    asset = check("POST", "/equipment", {"assetCode": f"T-{suffix}", "name": "Test Kayak", "category": "Kayak", "size": "M", "dailyRate": 100}, 201, token)["id"]
    check("POST", "/equipment", {"assetCode": f"T-{suffix}", "name": "Another", "category": "Kayak", "size": "L", "dailyRate": 50}, 409, token)
    start = datetime.now(timezone.utc).replace(hour=10, minute=0, second=0, microsecond=0) + timedelta(days=10)
    a, b, c = [x.isoformat().replace("+00:00", "Z") for x in (start, start+timedelta(days=1), start+timedelta(days=2))]
    rental = {"customerId": c1, "equipmentId": asset, "startAt": a, "endAt": b}
    book = check("POST", "/rentals", rental, 201, token)
    assert book["total"] == 100, book
    check("POST", "/rentals", rental, 409, token)
    check("POST", "/rentals", {**rental, "startAt": b, "endAt": c}, 201, token)
    check("POST", f"/rentals/{book['id']}/checkout", token=token)
    check("POST", f"/rentals/{book['id']}/return", token=token)
    check("POST", f"/rentals/{book['id']}/return", expected=409, token=token)
    print("[PASS] unique inventory, overlapping/adjacent rentals, state transitions, backend pricing")
    room = check("POST", "/rooms", {"code": f"R-{suffix}", "name": "Test oda", "description": "Test", "capacity": 2, "nightlyRate": 400}, 201, token)["id"]
    sd = (datetime.now(timezone.utc)+timedelta(days=20)).date()
    d1, d2, d3 = [x.isoformat()+"T00:00:00Z" for x in (sd, sd+timedelta(days=2), sd+timedelta(days=3))]
    stay = {"customerId": c1, "roomId": room, "startAt": d1, "endAt": d2, "guestCount": 2}
    stay_created = check("POST", "/stays", stay, 201, token)
    assert stay_created["total"] == 800
    check("POST", "/stays", stay, 409, token)
    check("POST", "/stays", {**stay, "guestCount": 3, "startAt": d2, "endAt": d3}, 400, token)
    check("POST", "/stays", {**stay, "startAt": d2, "endAt": d3}, 201, token)
    print("[PASS] room overlap prevention, capacity, adjacent dates")
    activity = check("POST", "/activities", {"name": "Test ders", "kind": "Lesson", "instructor": "Test instructor", "startsAt": a, "endsAt": b, "capacity": 1, "price": 100}, 201, token)
    check("POST", f"/activities/{activity['id']}/enroll", {"customerId": c1}, token=token)
    check("POST", f"/activities/{activity['id']}/enroll", {"customerId": c2}, 409, token)
    print("[PASS] activity capacity")
    payment = {"referenceType": "Rental", "referenceId": book["id"], "kind": "Charge", "method": "Cash", "amount": 40, "externalReference": f"P-{suffix}"}
    check("POST", "/payments", payment, 201, token)
    check("POST", "/payments", payment, 409, token)
    check("POST", "/payments", {**payment, "amount": 61, "externalReference": ""}, 409, token)
    check("POST", "/payments", {**payment, "amount": 60, "externalReference": ""}, 201, token)
    check("POST", "/payments", {**payment, "kind": "Refund", "amount": 101, "externalReference": ""}, 409, token)
    print("[PASS] payment idempotency, limit and refund checks")
    print("All integration scenarios passed on disposable test database.")


if __name__ == "__main__":
    main()

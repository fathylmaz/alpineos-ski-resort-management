"""Customer+staff interoperability and authorization checks. DISPOSABLE LOCAL DB ONLY.

Requires I_CONFIRM_DISPOSABLE_DB=YES and running docker compose.
Creates customers and test bookings. Do not run against a production database.
"""
from __future__ import annotations
import json
import os
import uuid
import urllib.error
import urllib.request
from datetime import datetime, timedelta, timezone
from http.cookiejar import CookieJar

BASE=os.getenv('SKIRESORT_URL','http://localhost:8088').rstrip('/')

class Client:
    def __init__(self):
        self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(CookieJar()))
        self.token=None
    def call(self,method,path,payload=None,expected=200):
        if self.token is None:self.csrf()
        headers={'Accept':'application/json'}
        if method!='GET':headers['X-CSRF-TOKEN']=self.token
        if payload is not None:headers['Content-Type']='application/json'
        request=urllib.request.Request(BASE+'/api'+path,
            data=json.dumps(payload).encode() if payload is not None else None,headers=headers,method=method)
        try:
            with self.opener.open(request,timeout=15) as response:
                status=response.status
                data=json.loads(response.read())
        except urllib.error.HTTPError as e:
            status=e.code;data=json.loads(e.read())
        assert status==expected,(method,path,status,expected,data)
        return data
    def csrf(self):
        request=urllib.request.Request(BASE+'/api/auth/csrf')
        with self.opener.open(request,timeout=15) as response:
            self.token=json.load(response)['token']

def main():
    assert os.getenv('I_CONFIRM_DISPOSABLE_DB')=='YES','Set I_CONFIRM_DISPOSABLE_DB=YES. This test writes data.'
    staff=Client()
    staff.call('POST','/auth/login',{'email':os.environ['ADMIN_EMAIL'],'password':os.environ['ADMIN_PASSWORD']})
    staff.csrf()
    suffix=uuid.uuid4().hex[:12]
    item=staff.call('POST','/equipment',{'assetCode':f'CST-{suffix}','name':'Müşteri Test Kayak','category':'Kayak','size':'170 cm','dailyRate':350},201)
    room=staff.call('POST','/rooms',{'code':f'CRT-{suffix}','name':'Müşteri Test Oda','description':'Test','capacity':2,'nightlyRate':900},201)
    now=datetime.now(timezone.utc).replace(hour=10,minute=0,second=0,microsecond=0)+timedelta(days=22)
    start=now.isoformat().replace('+00:00','Z');end=(now+timedelta(days=2)).isoformat().replace('+00:00','Z')
    stayday=now.date().isoformat()+'T00:00:00Z';checkout=(now+timedelta(days=3)).date().isoformat()+'T00:00:00Z'
    first=Client();second=Client()
    for idx,c in enumerate([first,second]):
        email=f'portal-{idx}-{suffix}@example.test'
        c.call('POST','/customer/register',{'name':f'Test Customer {idx}','email':email,'phone':'', 'password':'CustomerTestPassword2026!'},201)
        c.csrf()
        assert c.call('GET','/auth/me')['role']=='Customer'
        c.call('GET','/dashboard',expected=403)
        c.call('GET','/customers',expected=403)
        c.call('GET','/payments',expected=403)
        assert len(c.call('GET','/customer/rentals'))==0
    rented=first.call('POST','/customer/rentals',{'equipmentId':item['id'],'startAt':start,'endAt':end},201)
    assert rented['total']==700
    first.call('POST','/customer/rentals',{'equipmentId':item['id'],'startAt':start,'endAt':end},409)
    second.call('POST','/customer/rentals',{'equipmentId':item['id'],'startAt':start,'endAt':end},409)
    booked=first.call('POST','/customer/stays',{'roomId':room['id'],'startAt':stayday,'endAt':checkout,'guestCount':2},201)
    assert booked['total']==2700
    second.call('POST','/customer/stays',{'roomId':room['id'],'startAt':stayday,'endAt':checkout,'guestCount':1},409)
    second.call('POST',f"/customer/rentals/{rented['id']}/cancel",{'reason':'Foreign account'},404)
    second.call('POST',f"/customer/stays/{booked['id']}/cancel",{'reason':'Foreign account'},404)
    assert len(first.call('GET','/customer/rentals'))==1
    assert len(second.call('GET','/customer/rentals'))==0
    assert any(x['id']==rented['id'] for x in staff.call('GET','/rentals'))
    assert any(x['id']==booked['id'] for x in staff.call('GET','/stays'))
    first.call('POST',f"/customer/rentals/{rented['id']}/cancel",{'reason':'Plan change'},200)
    second.call('POST','/customer/rentals',{'equipmentId':item['id'],'startAt':start,'endAt':end},201)
    print('[PASS] Guest register; staff RBAC; ownership, dates, collision, admin visibility and cancellation.')

if __name__=='__main__':main()

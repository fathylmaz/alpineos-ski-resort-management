import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ChangeEvent, FormEvent, ReactNode } from 'react';
import {
  Activity, ArrowRight, ArrowUpRight, BedDouble, Building2, CalendarDays, Check,
  ChevronDown, ChevronRight, ClipboardList, CreditCard, DoorOpen, Download, GraduationCap,
  HardHat, LayoutDashboard, LogOut, Menu, MountainSnow, Package, Plus, RefreshCw, Search,
  ShieldCheck, Snowflake, Sparkles, UserCog, UsersRound, Wallet, X
} from 'lucide-react';
import { logout, refreshCsrf, request } from './api';
import GuestAccess from './GuestAccess';
import CustomerPortal from './CustomerPortal';
import type { Dashboard, Person, RecordRow } from './api';

type Section = 'dashboard' | 'rentals' | 'stays' | 'equipment' | 'customers' | 'rooms' |
  'slopes' | 'activities' | 'payments' | 'departments' | 'employees' | 'staff' | 'audit';
type Field = { key: string; label: string; kind?: 'text' | 'email' | 'password' | 'number' | 'date' | 'datetime' | 'textarea' | 'select'; source?: Section; choices?: string[]; required?: boolean; min?: number };
type Config = { name: string; plural: string; description: string; endpoint: string; columns: [string, string][]; fields: Field[]; canCreate?: string[] };
type Action = { label: string; url: string; method: string; fields?: Field[]; heading: string; variant?: string; defaults?: Record<string,string> };

const F = (key: string, label: string, kind: Field['kind'] = 'text', extra: Partial<Field> = {}): Field => ({ key, label, kind, required: true, ...extra });
const configs: Record<Exclude<Section, 'dashboard'>, Config> = {
  customers: { name:'Müşteri', plural:'Müşteriler', description:'Misafir iletişim ve kayıt yönetimi', endpoint:'/customers', canCreate:['Admin','Reception'],
    columns:[['fullName','Ad Soyad'],['email','E-posta'],['phone','Telefon'],['createdAt','Kayıt Tarihi']], fields:[F('fullName','Ad Soyad'),F('email','E-posta','email'),F('phone','Telefon'),F('notes','Notlar','textarea',{required:false})] },
  equipment: { name:'Ekipman', plural:'Ekipman Envanteri', description:'Tekil varlıklar, kiralama tarifeleri ve bakım kayıtları', endpoint:'/equipment', canCreate:['Admin','Equipment'],
    columns:[['assetCode','Varlık Kodu'],['name','Ekipman'],['category','Kategori'],['size','Beden'],['dailyRate','Günlük Ücret'],['status','Durum']],
    fields:[F('assetCode','Demirbaş kodu'),F('name','Ürün adı'),F('category','Kategori'),F('size','Beden / Ölçü'),F('dailyRate','Günlük ücret (₺)','number',{min:0})] },
  rentals: { name:'Kiralama', plural:'Ekipman Kiralamaları', description:'Rezervasyon, teslim, iade ve iptal süreçleri', endpoint:'/rentals', canCreate:['Admin','Reception'],
    columns:[['customer','Müşteri'],['assetCode','Ekipman Kodu'],['startAt','Başlangıç'],['endAt','Bitiş'],['total','Toplam'],['status','Durum']],
    fields:[F('customerId','Müşteri','select',{source:'customers'}),F('equipmentId','Ekipman','select',{source:'equipment'}),F('startAt','Başlangıç','datetime'),F('endAt','Bitiş','datetime')] },
  rooms: { name:'Konaklama Birimi', plural:'Odalar & Konaklama Birimleri', description:'Oda özellikleri, kapasite ve gecelik fiyatlar', endpoint:'/rooms', canCreate:['Admin','Reception'],
    columns:[['code','Oda Kodu'],['name','Oda'],['capacity','Kapasite'],['nightlyRate','Gecelik Ücret'],['isActive','Aktif']],
    fields:[F('code','Oda kodu'),F('name','Oda adı'),F('description','Açıklama','textarea',{required:false}),F('capacity','Kapasite','number',{min:1}),F('nightlyRate','Gecelik ücret (₺)','number',{min:0})] },
  stays: { name:'Konaklama', plural:'Konaklama Rezervasyonları', description:'Çakışmasız oda rezervasyonları ve giriş / çıkış', endpoint:'/stays', canCreate:['Admin','Reception'],
    columns:[['customer','Misafir'],['roomCode','Oda'],['startAt','Giriş'],['endAt','Çıkış'],['guestCount','Kişi'],['total','Toplam'],['status','Durum']],
    fields:[F('customerId','Müşteri','select',{source:'customers'}),F('roomId','Oda','select',{source:'rooms'}),F('startAt','Giriş tarihi','date'),F('endAt','Çıkış tarihi','date'),F('guestCount','Misafir sayısı','number',{min:1})] },
  slopes: { name:'Pist', plural:'Pist Durumları', description:'Pist kullanımı, kar kalınlığı ve hava gözlemleri', endpoint:'/slopes', canCreate:['Admin','Slope'],
    columns:[['name','Pist'],['difficulty','Zorluk'],['snowDepthCm','Kar (cm)'],['weather','Hava'],['status','Durum'],['updatedAt','Güncellendi']],
    fields:[F('name','Pist adı'),F('difficulty','Zorluk','select',{choices:['Easy','Medium','Hard']}),F('status','Durum','select',{choices:['Open','Closed','Maintenance']}),F('snowDepthCm','Kar kalınlığı (cm)','number',{min:0}),F('weather','Hava durumu')] },
  activities: { name:'Etkinlik', plural:'Kayak Dersleri & Etkinlikler', description:'Eğitmenler, yarışmalar, seans ve kontenjan yönetimi', endpoint:'/activities', canCreate:['Admin','Reception'],
    columns:[['name','Etkinlik'],['kind','Tür'],['instructor','Eğitmen'],['startsAt','Başlangıç'],['capacity','Kontenjan'],['enrolled','Kayıtlı'],['price','Ücret'],['status','Durum']],
    fields:[F('name','Etkinlik adı'),F('kind','Tür','select',{choices:['Lesson','Competition','Event']}),F('instructor','Eğitmen'),F('startsAt','Başlangıç','datetime'),F('endsAt','Bitiş','datetime'),F('capacity','Kontenjan','number',{min:1}),F('price','Katılım ücreti (₺)','number',{min:0})] },
  payments: { name:'Ödeme', plural:'Finans & Ödeme Kayıtları', description:'Manuel tahsilat / iade defteri; online ödeme altyapısı değildir', endpoint:'/payments', canCreate:['Admin','Cashier'],
    columns:[['customer','Müşteri'],['referenceType','İşlem'],['kind','Tür'],['method','Yöntem'],['amount','Tutar'],['createdAt','Tarih']],
    fields:[F('referenceType','İşlem türü','select',{choices:['Rental','Stay']}),F('referenceId','Rezervasyon','select',{source:'rentals'}),F('kind','Hareket','select',{choices:['Charge','Refund']}),F('method','Yöntem','select',{choices:['Cash','Card','BankTransfer']}),F('amount','Tutar (₺)','number',{min:0.01}),F('externalReference','Makbuz / idempotency referansı','text',{required:false})] },
  departments: { name:'Departman', plural:'Departmanlar', description:'İşletme organizasyon birimleri', endpoint:'/departments', canCreate:['Admin'],
    columns:[['name','Departman'],['createdAt','Oluşturulma']], fields:[F('name','Departman adı')] },
  employees: { name:'Personel', plural:'Personel Yönetimi', description:'Çalışan kayıtları, görevler ve departmanlar', endpoint:'/employees', canCreate:['Admin'],
    columns:[['fullName','Ad Soyad'],['position','Pozisyon'],['department','Departman'],['email','E-posta']],
    fields:[F('fullName','Ad Soyad'),F('position','Pozisyon'),F('email','E-posta','email'),F('departmentId','Departman','select',{source:'departments',required:false})] },
  staff: { name:'Personel Hesabı', plural:'Kullanıcı Hesapları & Roller', description:'Yönetim paneline giriş yapan ekip üyeleri', endpoint:'/auth/staff', canCreate:['Admin'],
    columns:[['displayName','Ad Soyad'],['email','E-posta'],['role','Yetki'],['isActive','Aktif']],
    fields:[F('displayName','Ad Soyad'),F('email','E-posta','email'),F('role','Yetki','select',{choices:['Admin','Reception','Equipment','Slope','Cashier']}),F('password','Güçlü parola (12+ karakter)','password')] },
  audit: { name:'Denetim Kaydı', plural:'Denetim Günlüğü', description:'Kritik işlemlerin kim tarafından ne zaman yapıldığı', endpoint:'/dashboard/audit',
    columns:[['createdAt','Tarih'],['actor','Kullanıcı'],['action','İşlem'],['entity','Kayıt Türü'],['entityId','Referans']],fields:[] }
};
const primaryNav: { title: string; items: { id: Section; label: string; icon: typeof LayoutDashboard }[] }[] = [
  { title:'GENEL BAKIŞ', items:[{id:'dashboard',label:'Kontrol Paneli',icon:LayoutDashboard}] },
  { title:'REZERVASYON & MÜŞTERİ', items:[{id:'rentals',label:'Ekipman Kiralama',icon:CalendarDays},{id:'stays',label:'Konaklama',icon:BedDouble},{id:'customers',label:'Müşteriler',icon:UsersRound}] },
  { title:'TESİS YÖNETİMİ', items:[{id:'equipment',label:'Ekipman Envanteri',icon:Package},{id:'rooms',label:'Odalar',icon:DoorOpen},{id:'slopes',label:'Pist Durumları',icon:MountainSnow},{id:'activities',label:'Dersler & Etkinlikler',icon:GraduationCap}] },
  { title:'İŞLETME', items:[{id:'payments',label:'Finans Kayıtları',icon:CreditCard},{id:'employees',label:'Personel',icon:HardHat},{id:'departments',label:'Departmanlar',icon:Building2},{id:'staff',label:'Yetkilendirme',icon:UserCog},{id:'audit',label:'Denetim Günlüğü',icon:ClipboardList}] }
];
const trMoney=(value: unknown) => new Intl.NumberFormat('tr-TR',{style:'currency',currency:'TRY',maximumFractionDigits:2}).format(Number(value||0));
const trDate=(value: unknown) => value ? new Date(String(value)).toLocaleString('tr-TR',{day:'2-digit',month:'short',year:'numeric',hour:'2-digit',minute:'2-digit',timeZone:'Europe/Istanbul'}) : '—';
const todayIso=()=>new Date().toISOString().slice(0,10);
const statusText: Record<string,string> = {Available:'Müsait',Maintenance:'Bakımda',Retired:'Kullanım dışı',Reserved:'Rezerve',CheckedOut:'Teslim edildi',Returned:'İade edildi',Cancelled:'İptal',CheckedIn:'Giriş yapıldı',Completed:'Tamamlandı',Open:'Açık',Closed:'Kapalı',Scheduled:'Planlandı',Easy:'Kolay',Medium:'Orta',Hard:'Zor',Lesson:'Ders',Competition:'Yarışma',Event:'Etkinlik',Charge:'Tahsilat',Refund:'İade',Cash:'Nakit',Card:'Kart',BankTransfer:'Havale',Enrolled:'Kayıtlı'};
const badgeType=(s:string)=>['Available','Open','Returned','Completed','Charge','CheckedIn'].includes(s)?'ok':['Cancelled','Closed','Retired','Refund'].includes(s)?'bad':['Maintenance','CheckedOut'].includes(s)?'warn':'blue';
const isMoney=(key:string)=>['dailyRate','nightlyRate','total','price','amount'].includes(key);
const isDate=(key:string)=>['createdAt','startAt','endAt','startsAt','endsAt','updatedAt'].includes(key);
const tableValue=(key:string,value:unknown):ReactNode=>{
  if(value===null||value===undefined||value==='')return <span className="muted">—</span>;
  if(typeof value==='boolean')return <span className={`pill ${value?'ok':'bad'}`}>{value?'Evet':'Hayır'}</span>;
  if(isMoney(key))return <strong>{trMoney(value)}</strong>;
  if(isDate(key))return trDate(value);
  if(key==='status'||key==='kind'||key==='difficulty')return <span className={`pill ${badgeType(String(value))}`}><span className="pill-dot"/>{statusText[String(value)]||String(value)}</span>;
  return <span>{statusText[String(value)]||String(value)}</span>;
};
function permitted(role:string, roles?:string[]){return Boolean(roles?.includes(role));}

function Login({onSuccess,onGuest}:{onSuccess:(person:Person)=>void;onGuest:()=>void}) {
  const [email,setEmail]=useState('');const [password,setPassword]=useState('');const [error,setError]=useState('');const [busy,setBusy]=useState(false);
  async function submit(event:FormEvent){event.preventDefault();setBusy(true);setError('');try{
    const person=await request<Person>('/auth/login','POST',{email,password});await refreshCsrf();onSuccess(person);
  }catch(err){setError((err as Error).message);}finally{setBusy(false);}}
  return <div className="login-screen"><div className="login-left"><div className="brand light"><span className="brand-symbol"><MountainSnow size={26}/></span><span><b>ALPINE<span className="brand-accent">OS</span></b><small>RESORT MANAGEMENT</small></span></div>
    <div className="mountain-drawing"><div className="sky-glow"/><div className="mountain-one"/><div className="mountain-two"/><div className="mountain-three"/></div>
    <div className="login-pitch"><span className="small-caps">ONE RESORT. ONE PLATFORM.</span><h1>Dağdaki tüm operasyonlar, <em>tek merkezde.</em></h1><p>Rezervasyonlardan pist yönetimine, ekipmanlardan finans kayıtlarına kadar her şey kontrol altında.</p></div>
    <span className="login-copyright">© {new Date().getFullYear()} AlpineOS · Kayak Merkezi Otomasyonu</span></div>
    <div className="login-right"><div className="login-card"><div className="mobile-brand"><MountainSnow/> AlpineOS</div><div className="login-tag"><ShieldCheck size={14}/> Güvenli personel girişi</div><h2>Tekrar hoş geldiniz.</h2><p>Yönetim paneline erişmek için hesabınıza giriş yapın.</p>
    <form onSubmit={submit}><label>E-posta adresi<input type="email" autoComplete="username" required placeholder="ornek@kayakmerkezi.com" value={email} onChange={e=>setEmail(e.target.value)}/></label><label>Parola<input type="password" autoComplete="current-password" required placeholder="••••••••••••" value={password} onChange={e=>setPassword(e.target.value)}/></label>
    {error&&<div className="message error">{error}</div>}<button className="btn btn-primary login-button" disabled={busy}>{busy?'Giriş yapılıyor...':'Yönetim Paneline Giriş'}<ArrowRight size={18}/></button></form>
    <button className="guest-admin-link" onClick={onGuest}><UsersRound size={17}/> Misafir / Müşteri Paneline Geç <ArrowRight size={15}/></button><p className="login-hint">Hesap bilgileri sistem yöneticiniz tarafından oluşturulur. İlk kurulum yöneticisi <code>.env</code> dosyasında tanımlanır.</p></div></div></div>;
}
function Modal({title,subtitle,fields,lookups,defaults,onClose,onSave,saving}:{title:string;subtitle:string;fields:Field[];lookups:Partial<Record<Section,RecordRow[]>>;defaults?:Record<string,string>;onClose:()=>void;onSave:(data:Record<string,unknown>)=>Promise<void>;saving:boolean}){
  const [values,setValues]=useState<Record<string,string>>(()=>defaults||{});
  const [error,setError]=useState('');
  const selectedReference=values.referenceType||'Rental';
  async function submit(event:FormEvent){event.preventDefault();setError('');const data:Record<string,unknown>={};
    for(const field of fields){const raw=values[field.key]??'';
      if(!raw&&!field.required){data[field.key]=field.kind==='select'?null:'';continue;}
      data[field.key]=field.kind==='number'?Number(raw):field.kind==='datetime'?new Date(raw).toISOString():field.kind==='date'?`${raw}T00:00:00.000Z`:raw;
    }
    try{await onSave(data);}catch(e){setError((e as Error).message);}}
  function choices(field:Field):{value:string;label:string}[]{
    if(field.choices)return field.choices.map(s=>({value:s,label:statusText[s]||s}));
    let source=field.source;if(field.key==='referenceId') source=selectedReference==='Stay'?'stays':'rentals';
    return (source?lookups[source]||[]:[]).map(r=>({value:String(r.id),label:
      source==='customers'?String(r.fullName):source==='equipment'?`${r.assetCode} · ${r.name}`:source==='rooms'?`${r.code} · ${r.name}`:
      source==='departments'?String(r.name):source==='rentals'||source==='stays'?`${String(r.customer||'').slice(0,22)} · ${trMoney(r.total)} · ${String(r.id).slice(0,8)}`:String(r.name||r.id)}));
  }
  return <div className="overlay" onClick={onClose}><section className="modal" role="dialog" aria-modal="true" aria-label={title} onClick={e=>e.stopPropagation()}>
      <header className="modal-head"><div><span className="modal-eyebrow">ALPINEOS OPERATIONS</span><h2>{title}</h2><p>{subtitle}</p></div><button className="icon-button" onClick={onClose} aria-label="Kapat"><X size={19}/></button></header>
      <form onSubmit={submit}><div className="form-grid">{fields.map(field=>{const opts=choices(field);const inputProps={required:field.required!==false,value:values[field.key]??'',onChange:(e:ChangeEvent<HTMLInputElement|HTMLTextAreaElement|HTMLSelectElement>)=>setValues(v=>({...v,[field.key]:e.target.value,...(field.key==='referenceType'?{referenceId:''}:{})}))};
        return <label key={field.key} className={field.kind==='textarea'?'wide':''}>{field.label}{field.kind==='select'?<select {...inputProps}><option value="">Seçiniz</option>{opts.map(o=><option key={o.value} value={o.value}>{o.label}</option>)}</select>:field.kind==='textarea'?<textarea {...inputProps} rows={3} placeholder={field.label}/>:<input {...inputProps} type={field.kind==='number'?'number':field.kind==='date'?'date':field.kind==='datetime'?'datetime-local':field.kind==='email'?'email':field.kind==='password'?'password':'text'} min={field.min} step={field.kind==='number'?'any':undefined} placeholder={field.label}/>}</label>;
      })}</div>{error&&<div className="message error">{error}</div>}
      <footer className="modal-footer"><button className="btn btn-quiet" type="button" onClick={onClose}>Vazgeç</button><button className="btn btn-primary" type="submit" disabled={saving}>{saving?'Kaydediliyor...':'Kaydet'}<Check size={17}/></button></footer></form>
    </section></div>;
}
function DashboardPage({data,onNavigate}:{data:Dashboard|null;onNavigate:(page:Section)=>void}){
  if(!data)return <div className="empty">Gösterge verileri yükleniyor...</div>;
  const max=Math.max(...data.trend.map(x=>Math.abs(x.value)),1);
  const metrics=[{label:'Toplam Müşteri',value:data.customers,icon:UsersRound,change:'Kayıtlı profiller',tone:'mint'},
    {label:'Aktif Kiralama',value:data.activeRentals,icon:Package,change:`${data.pendingRentals} rezerve`,tone:'blue'},
    {label:'Konaklama Doluluğu',value:`${data.occupiedRooms} / ${data.rooms}`,icon:BedDouble,change:`${data.upcomingStays} bekleyen`,tone:'yellow'},
    {label:'Net Tahsilat',value:trMoney(data.netReceived),icon:Wallet,change:'Manuel kayıtlar',tone:'purple'}];
  return <><section className="welcome-banner"><div className="welcome-copy"><span className="eyebrow"><Sparkles size={14}/> TESİS OPERASYON MERKEZİ</span><h2>Her şey kontrol altında.<br/><em>Sezona hazırız.</em></h2><p>Kayak merkezinizin günlük operasyonlarına tek ekrandan hakim olun.</p><button className="banner-button" onClick={()=>onNavigate('rentals')}>Yeni kiralama oluştur <ArrowRight size={15}/></button></div><div className="banner-art"><div className="banner-sun"/><div className="banner-mountain-a"/><div className="banner-mountain-b"/><div className="banner-mountain-c"/><span className="floating-tag"><Snowflake size={16}/> Alpine season</span></div></section>
    <div className="metric-grid">{metrics.map(m=><div className="metric-card" key={m.label}><div className="metric-top"><span>{m.label}</span><span className={`metric-icon ${m.tone}`}><m.icon size={19}/></span></div><div className="metric-value">{m.value}</div><div className="metric-foot"><span className="tiny-indicator"/>{m.change}</div></div>)}</div>
    <div className="insight-grid"><section className="panel chart-panel"><div className="panel-header"><div><p className="section-eyebrow">FİNANSAL GÖRÜNÜM</p><h3>Son 7 Günlük Tahsilat</h3></div><span className="date-range">7 GÜN <ChevronDown size={14}/></span></div>
      <div className="chart-y-label">Net işlem toplamı (₺)</div><div className="bar-chart">{data.trend.map(day=><div className="bar-column" key={day.date}><div className="bar-area"><div className="bar" title={trMoney(day.value)} style={{height:`${Math.max(5,Math.abs(day.value)/max*100)}%`}}/></div><span>{new Date(`${day.date}T12:00:00`).toLocaleDateString('tr-TR',{weekday:'short'})}</span></div>)}</div>
      <div className="chart-summary"><div><span>Net Tahsilat</span><b>{trMoney(data.netReceived)}</b></div><div><span>Hesaplanan Açık Bakiye</span><b>{trMoney(data.pendingBalance)}</b></div></div></section>
      <section className="panel resort-panel"><div className="panel-header"><div><p className="section-eyebrow">CANLI OPERASYON</p><h3>Tesis Durumu</h3></div><span className="live-chip"><span/> Güncel</span></div>
        <div className="resort-stat"><span className="icon-box green"><MountainSnow size={19}/></span><div><b>Açık Pistler</b><small>Kayak alanı durumu</small></div><strong>{data.openSlopes}<span> / {data.totalSlopes}</span></strong></div>
        <div className="resort-stat"><span className="icon-box blue"><Package size={19}/></span><div><b>Uygun Ekipman</b><small>Kiralama envanteri</small></div><strong>{data.availableEquipment}<span> / {data.equipmentCount}</span></strong></div>
        <div className="resort-stat"><span className="icon-box orange"><BedDouble size={19}/></span><div><b>Konaklayan Misafirler</b><small>Dolu odalar</small></div><strong>{data.occupiedRooms}<span> / {data.rooms}</span></strong></div>
        <button className="text-link" onClick={()=>onNavigate('slopes')}>Pistleri görüntüle <ArrowRight size={15}/></button></section></div>
    <div className="insight-grid lower"><section className="panel"><div className="panel-header"><div><p className="section-eyebrow">YAKLAŞAN PROGRAM</p><h3>Dersler & Etkinlikler</h3></div><button className="icon-button" aria-label="Etkinliklere git" onClick={()=>onNavigate('activities')}><ArrowUpRight size={18}/></button></div>
      {data.upcoming.length===0?<div className="simple-empty">Yaklaşan etkinlik bulunmuyor.</div>:data.upcoming.map(a=><div className="event-line" key={a.id}><span className="event-date"><b>{new Date(a.startsAt).toLocaleDateString('tr-TR',{day:'2-digit',timeZone:'Europe/Istanbul'})}</b><small>{new Date(a.startsAt).toLocaleDateString('tr-TR',{month:'short',timeZone:'Europe/Istanbul'})}</small></span><div><b>{a.name}</b><small>{a.instructor} · {a.capacity} kişilik</small></div><ChevronRight size={17}/></div>)}</section>
      <section className="panel"><div className="panel-header"><div><p className="section-eyebrow">SON HAREKETLER</p><h3>İşlem Geçmişi</h3></div><span className="neutral-tag">İZLENEBİLİR</span></div>
      {data.recent.length===0?<div className="simple-empty">Henüz işlem kaydı yok.</div>:data.recent.slice(0,5).map(log=><div className="log-line" key={log.id}><span className="log-blob"><Activity size={14}/></span><div><b>{log.action} · {log.entity}</b><small>{log.actor}</small></div><time>{trDate(log.createdAt)}</time></div>)}</section></div>
  </>;
}
function exportCsv(rows:RecordRow[],columns:[string,string][],name:string){
  const esc=(v:unknown)=>{const raw=String(v??'');const safe=/^[=+@\-\t\r]/.test(raw)?`'${raw}`:raw;return `"${safe.replace(/"/g,'""')}"`;};
  const csv='\uFEFF'+[columns.map(c=>esc(c[1])).join(';'),...rows.map(r=>columns.map(c=>esc(r[c[0]])).join(';'))].join('\r\n');
  const url=URL.createObjectURL(new Blob([csv],{type:'text/csv;charset=utf-8'}));const a=document.createElement('a');a.href=url;a.download=`${name}-${todayIso()}.csv`;a.click();URL.revokeObjectURL(url);
}
function getActions(page:Section,row:RecordRow,role:string):Action[]{
  const id=row.id;const s=String(row.status??'');
  if(page==='rentals'&&permitted(role,['Admin','Reception']))return [
    ...(s==='Reserved'?[{heading:'Kiralama teslimatı',label:'Teslim et',url:`/rentals/${id}/checkout`,method:'POST'},
      {heading:'Kiralama iptali',label:'İptal et',url:`/rentals/${id}/cancel`,method:'POST',fields:[F('reason','İptal nedeni','textarea')],variant:'danger'}]:[]),
    ...(s==='CheckedOut'?[{heading:'Ekipman iadesi',label:'İade al',url:`/rentals/${id}/return`,method:'POST'}]:[])];
  if(page==='stays'&&permitted(role,['Admin','Reception']))return [
    ...(s==='Reserved'?[{heading:'Konaklama girişi',label:'Giriş yap',url:`/stays/${id}/checkin`,method:'POST'},
      {heading:'Rezervasyon iptali',label:'İptal et',url:`/stays/${id}/cancel`,method:'POST',fields:[F('reason','İptal nedeni','textarea')],variant:'danger'}]:[]),
    ...(s==='CheckedIn'?[{heading:'Konaklama çıkışı',label:'Çıkış yap',url:`/stays/${id}/checkout`,method:'POST'}]:[])];
  if(page==='equipment'&&permitted(role,['Admin','Equipment']))return [
    {heading:'Ekipman durumu',label:'Durumu değiştir',url:`/equipment/${id}/status`,method:'PATCH',fields:[F('status','Yeni durum','select',{choices:['Available','Maintenance','Retired']})],defaults:{status:s}},
    {heading:'Bakım kaydı',label:'Bakım ekle',url:`/equipment/${id}/maintenance`,method:'POST',fields:[F('description','Bakım açıklaması','textarea'),F('cost','Maliyet (₺)','number',{min:0})]}];
  if(page==='slopes'&&permitted(role,['Admin','Slope']))return [{heading:'Pist durumunu güncelle',label:'Durumu değiştir',url:`/slopes/${id}/status`,method:'PATCH',fields:[F('status','Durum','select',{choices:['Open','Closed','Maintenance']}),F('snowDepthCm','Kar kalınlığı','number',{min:0}),F('weather','Hava durumu')],defaults:{status:s,snowDepthCm:String(row.snowDepthCm??0),weather:String(row.weather??'')}}];
  if(page==='activities'&&permitted(role,['Admin','Reception']))return [{heading:'Etkinliğe müşteri kaydı',label:'Katılımcı ekle',url:`/activities/${id}/enroll`,method:'POST',fields:[F('customerId','Müşteri','select',{source:'customers'})]}];
  return [];
}
function TablePage({page,rows,loading,role,onNew,onAction,onReload,query,setQuery}:{page:Exclude<Section,'dashboard'>;rows:RecordRow[];loading:boolean;role:string;onNew:()=>void;onAction:(a:Action)=>void;onReload:()=>void;query:string;setQuery:(value:string)=>void}){
  const c=configs[page];const canCreate=permitted(role,c.canCreate);const filtered=rows.filter(r=>JSON.stringify(r).toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr')));
  return <section className="panel table-panel"><div className="table-toolbar"><div className="search-field"><Search size={18}/><input aria-label="Tabloda ara" placeholder="Kayıtlar arasında ara..." value={query} onChange={e=>setQuery(e.target.value)}/></div><div className="toolbar-actions"><button className="btn btn-quiet" title="Yenile" onClick={onReload}><RefreshCw size={16}/><span className="hide-small">Yenile</span></button><button className="btn btn-quiet" onClick={()=>exportCsv(filtered,c.columns,page)}><Download size={16}/><span className="hide-small">CSV Dışa Aktar</span></button>{canCreate&&<button className="btn btn-primary" onClick={onNew}><Plus size={17}/><span>Yeni {c.name}</span></button>}</div></div>
  <div className="table-wrap"><table><thead><tr>{c.columns.map(([,name])=><th key={name}>{name}</th>)}<th>İŞLEMLER</th></tr></thead><tbody>{!loading&&filtered.map(row=><tr key={row.id}>{c.columns.map(([key])=><td key={key}>{tableValue(key,row[key])}</td>)}<td><div className="row-actions">{getActions(page,row,role).map(a=><button key={a.label} className={`row-action ${a.variant==='danger'?'danger':''}`} onClick={()=>onAction(a)}>{a.label}</button>)}{getActions(page,row,role).length===0&&<span className="muted">—</span>}</div></td></tr>)}</tbody></table>
    {loading?<div className="table-empty">Veriler yükleniyor...</div>:filtered.length===0?<div className="table-empty"><span className="empty-icon"><Search size={26}/></span><b>Kayıt bulunamadı</b><span>Arama kriterini değiştirin veya yeni kayıt ekleyin.</span></div>:null}</div>
  <div className="table-footer"><span>{filtered.length} kayıt görüntüleniyor</span><span>AlpineOS Management · {new Date().getFullYear()}</span></div></section>;
}

export default function App(){
  const [person,setPerson]=useState<Person|null>(null);const [checking,setChecking]=useState(true);
  const [guestLogin,setGuestLogin]=useState(false);
  const [page,setPage]=useState<Section>('dashboard');const [rows,setRows]=useState<RecordRow[]>([]);const [dashboard,setDashboard]=useState<Dashboard|null>(null);
  const [lookups,setLookups]=useState<Partial<Record<Section,RecordRow[]>>>({});const [loading,setLoading]=useState(false);
  const [query,setQuery]=useState('');const [modal,setModal]=useState<{title:string;subtitle:string;fields:Field[];url:string;method:string;defaults?:Record<string,string>}|null>(null);
  const [saving,setSaving]=useState(false);const [notice,setNotice]=useState('');const [menuOpen,setMenuOpen]=useState(false);
  useEffect(()=>{refreshCsrf().then(()=>request<Person>('/auth/me')).then(setPerson).catch(()=>setPerson(null)).finally(()=>setChecking(false));},[]);
  const allowedPage=(id:Section)=>!(id==='staff'||id==='audit'||id==='departments'||id==='employees')||person?.role==='Admin';
  const load=useCallback(async(current:Section)=>{
    setLoading(true);try{if(current==='dashboard'){setDashboard(await request<Dashboard>('/dashboard'));}
      else{setRows(await request<RecordRow[]>(configs[current].endpoint));}
    }catch(err){if((err as Error).message.includes('401'))setPerson(null);else setNotice((err as Error).message);}finally{setLoading(false);}
  },[]);
  useEffect(()=>{if(person&&allowedPage(page))void load(page);setQuery('');},[person,page,load]);
  useEffect(()=>{if(!person)return;const sources:Section[]=['customers','equipment','rooms','rentals','stays','departments'];
    void Promise.all(sources.map(async p=>{try{return [p,await request<RecordRow[]>(configs[p as Exclude<Section,'dashboard'>].endpoint)] as const;}catch{return [p,[]] as const;}})).then(arr=>setLookups(Object.fromEntries(arr) as Partial<Record<Section,RecordRow[]>>));
  },[person,rows.length]);
  const changePage=(to:Section)=>{setPage(to);setMenuOpen(false);setNotice('');window.scrollTo({top:0,behavior:'smooth'});};
  const openNew=()=>{if(page==='dashboard')return;const config=configs[page];setModal({title:`Yeni ${config.name}`,subtitle:`${config.name} kaydını oluşturun. Tüm bilgiler sunucu tarafında doğrulanır.`,fields:config.fields,url:config.endpoint,method:'POST'});};
  const onAction=(a:Action)=>setModal({title:a.heading,subtitle:'İşlemi onaylayarak kaydı güncelleyin.',fields:a.fields||[],url:a.url,method:a.method,defaults:a.defaults});
  const save=async(data:Record<string,unknown>)=>{
    if(!modal)return;setSaving(true);try{await request(modal.url,modal.method,Object.keys(data).length?data:undefined);
      setModal(null);setNotice('İşlem başarıyla kaydedildi.');await load(page);
    }finally{setSaving(false);}
  };
  const current=page==='dashboard'?null:configs[page];
  const dateTitle=useMemo(()=>new Intl.DateTimeFormat('tr-TR',{weekday:'long',day:'numeric',month:'long',year:'numeric',timeZone:'Europe/Istanbul'}).format(new Date()),[]);
  if(checking)return <div className="boot"><Snowflake size={30}/><h2>AlpineOS</h2><p>Güvenli oturum kontrol ediliyor...</p></div>;
  if(!person)return guestLogin?<GuestAccess onSuccess={p=>{setPerson(p);setPage('dashboard');}} onStaff={()=>setGuestLogin(false)}/>:<Login onSuccess={p=>{setPerson(p);setPage('dashboard');}} onGuest={()=>setGuestLogin(true)}/>;
  if(person.role==='Customer')return <CustomerPortal person={person} onLogout={()=>setPerson(null)}/>;
  return <div className="app-shell"><aside className={`sidebar ${menuOpen?'show':''}`}><div className="brand"><span className="brand-symbol"><MountainSnow size={24}/></span><span><b>ALPINE<span className="brand-accent">OS</span></b><small>RESORT MANAGEMENT</small></span></div>
    <div className="sidebar-resort"><div className="resort-avatar"><MountainSnow size={20}/></div><div><b>Alpine Mountain</b><small>Kayak Merkezi</small></div><ChevronDown size={16}/></div>
    <nav className="nav">{primaryNav.map(group=><div className="nav-group" key={group.title}><span className="nav-heading">{group.title}</span>{group.items.filter(it=>allowedPage(it.id)).map(it=><button key={it.id} onClick={()=>changePage(it.id)} className={`nav-item ${page===it.id?'selected':''}`}><it.icon size={18}/><span>{it.label}</span>{page===it.id&&<span className="nav-active"/>}</button>)}</div>)}</nav>
    <div className="sidebar-bottom"><div className="support-box"><Snowflake size={21}/><b>Sezon yönetimi artık daha kolay.</b><p>AlpineOS ile her şey bir arada.</p></div><button className="user-profile" onClick={async()=>{try{await logout();setPerson(null);}catch(e){setNotice((e as Error).message);}}}><span className="user-avatar">{person.name.slice(0,1).toUpperCase()}</span><span><b>{person.name}</b><small>{person.role}</small></span><LogOut size={16}/></button></div></aside>
    {menuOpen&&<button className="mobile-overlay" aria-label="Menüyü kapat" onClick={()=>setMenuOpen(false)}/>}
    <main className="main"><header className="topbar"><div className="breadcrumb"><button className="mobile-menu" aria-label="Menüyü aç" onClick={()=>setMenuOpen(true)}><Menu size={22}/></button><span>AlpineOS</span><ChevronRight size={15}/><b>{page==='dashboard'?'Kontrol Paneli':current?.plural}</b></div><div className="topbar-right"><span className="topbar-date">{dateTitle}</span><span className="role-label"><ShieldCheck size={14}/>{person.role}</span><span className="top-avatar">{person.name.slice(0,1).toUpperCase()}</span></div></header>
    <div className="content"><div className="page-heading"><div><span className="heading-eyebrow">ALPINEOS / {page.toUpperCase()}</span><h1>{page==='dashboard'?'Kontrol Paneli':current?.plural}</h1><p>{page==='dashboard'?'Kayak merkezinizin güncel performansı ve operasyonel özeti.':current?.description}</p></div><div className="heading-right"><span className="online-dot"/> Operasyon Paneli</div></div>
      {notice&&<div className="message info"><Check size={16}/>{notice}<button aria-label="Bildirimi kapat" onClick={()=>setNotice('')}><X size={15}/></button></div>}
      {page==='dashboard'?<DashboardPage data={dashboard} onNavigate={changePage}/>:<TablePage key={page} page={page} rows={rows} loading={loading} role={person.role} onNew={openNew} onAction={onAction} onReload={()=>void load(page)} query={query} setQuery={setQuery}/>}
      <footer className="app-footer"><span>© {new Date().getFullYear()} AlpineOS · Kayak Merkezi Yönetim Sistemi</span><span><ShieldCheck size={13}/> Güvenli personel oturumu</span></footer>
    </div></main>
    {modal&&<Modal key={modal.url+modal.title} title={modal.title} subtitle={modal.subtitle} fields={modal.fields} lookups={lookups} defaults={modal.defaults} onClose={()=>setModal(null)} saving={saving} onSave={save}/>}
  </div>;
}

import { useCallback, useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  ArrowLeft, ArrowRight, BedDouble, CalendarCheck, CalendarDays, Check, ChevronRight,
  CircleUserRound, Clock3, CreditCard, GraduationCap, LayoutDashboard,
  LogOut, MapPin, Menu, MountainSnow, Package, Search, ShieldCheck, Snowflake,
  UserRound, Wallet, Wind, X
} from 'lucide-react';
import { logout, request } from './api';
import type { Person } from './api';
import './customer.css';

type Panel = 'home' | 'equipment' | 'rooms' | 'lessons' | 'slopes' | 'bookings' | 'payments' | 'profile';
type Item = Record<string, unknown> & { id:string };
type Catalog = { equipment:Item[]; rooms:Item[]; activities:Item[]; slopes:Item[] };
type Profile = {id:string; name:string;email:string;phone:string;createdAt:string};
type Booking = { kind:'rental'|'stay'|'activity'; item:Item };
const amount=(n:unknown)=>new Intl.NumberFormat('tr-TR',{style:'currency',currency:'TRY',maximumFractionDigits:0}).format(Number(n||0));
const dateText=(d:unknown)=>d?new Date(String(d)).toLocaleDateString('tr-TR',{day:'numeric',month:'short',year:'numeric',timeZone:'Europe/Istanbul'}):'—';
const dateTime=(d:unknown)=>d?new Date(String(d)).toLocaleString('tr-TR',{day:'numeric',month:'short',hour:'2-digit',minute:'2-digit',timeZone:'Europe/Istanbul'}):'—';
const labels: Record<string, string> = {Reserved:'Rezerve',CheckedOut:'Teslim edildi',Returned:'İade edildi',Cancelled:'İptal',Completed:'Tamamlandı',
  CheckedIn:'Giriş yapıldı',Enrolled:'Kayıtlı',Open:'Açık',Closed:'Kapalı',Maintenance:'Bakımda',
  Easy:'Başlangıç',Medium:'Orta',Hard:'İleri',Lesson:'Kayak dersi',Competition:'Yarışma',Event:'Etkinlik',
  Charge:'Tahsilat',Refund:'İade',Cash:'Nakit',Card:'Kart',BankTransfer:'Havale'};
const label=(v:unknown)=>labels[String(v)]||String(v);
const leftNav:{id:Panel;name:string;icon:typeof LayoutDashboard}[]=[
  {id:'home',name:'Keşfet',icon:LayoutDashboard},{id:'equipment',name:'Ekipman kirala',icon:Package},
  {id:'rooms',name:'Konaklama',icon:BedDouble},{id:'lessons',name:'Dersler ve etkinlikler',icon:GraduationCap},
  {id:'slopes',name:'Pist durumu',icon:MountainSnow},{id:'bookings',name:'Rezervasyonlarım',icon:CalendarCheck},
  {id:'payments',name:'Ödeme geçmişim',icon:CreditCard},{id:'profile',name:'Hesabım',icon:UserRound}
];
const tomorrow=()=>{const x=new Date();x.setDate(x.getDate()+2);return `${x.getFullYear()}-${String(x.getMonth()+1).padStart(2,'0')}-${String(x.getDate()).padStart(2,'0')}`;};
const localClock=(dayOffset=2)=>{const d=new Date();d.setDate(d.getDate()+dayOffset);d.setHours(10,0,0,0);return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}T10:00`;};
const iconFor=(category:string)=>category==='Snowboard'?'🏂':category==='Kask'?'⛑️':category==='Bot'?'🥾':category==='Gözlük'?'🥽':category==='Giyim'?'🧥':category==='Baton'?'🎿':category==='Koruma'?'🛡️':'⛷️';

function BookDialog({booking,onClose,onComplete}:{booking:Booking;onClose:()=>void;onComplete:()=>Promise<void>}){
  const [start,setStart]=useState(booking.kind==='stay'?tomorrow():localClock());
  const [end,setEnd]=useState(booking.kind==='stay'?(()=>{const d=new Date(`${tomorrow()}T12:00:00`);d.setDate(d.getDate()+3);return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;})():localClock(4));
  const [guests,setGuests]=useState(1);const [error,setError]=useState('');const [saving,setSaving]=useState(false);
  const [availability,setAvailability]=useState<{available:boolean;reason:string}|null>(null);
  const item=booking.item;const type=booking.kind;const rate=Number(type==='stay'?item.nightlyRate:item.dailyRate)||0;
  const days=Math.max(0,Math.ceil((new Date(end).getTime()-new Date(start).getTime())/86400000));
  useEffect(()=>{
    if(type==='activity'||days<=0){setAvailability(null);return;}
    let cancelled=false;
    setAvailability(null);
    try{
      const from=type==='stay'?`${start}T00:00:00.000Z`:new Date(start).toISOString();
      const until=type==='stay'?`${end}T00:00:00.000Z`:new Date(end).toISOString();
      const params=new URLSearchParams({kind:type,resourceId:item.id,startAt:from,endAt:until});
      void request<{available:boolean;reason:string}>(`/customer/availability?${params.toString()}`)
        .then(value=>{if(!cancelled)setAvailability(value);})
        .catch(()=>{if(!cancelled)setAvailability(null);});
    }catch {setAvailability(null);}
    return ()=>{cancelled=true;};
  },[type,item.id,start,end,days]);
  async function submit(event:FormEvent){event.preventDefault();setError('');setSaving(true);
    try{
      if(type==='rental')await request('/customer/rentals','POST',{equipmentId:item.id,startAt:new Date(start).toISOString(),endAt:new Date(end).toISOString()});
      else if(type==='stay')await request('/customer/stays','POST',{roomId:item.id,startAt:`${start}T00:00:00.000Z`,endAt:`${end}T00:00:00.000Z`,guestCount:guests});
      else await request(`/customer/activities/${item.id}/enroll`,'POST');
      await onComplete();
    }catch(e){setError((e as Error).message);}finally{setSaving(false);}
  }
  return <div className="cp-overlay" onMouseDown={onClose}><div className="cp-dialog" role="dialog" aria-modal="true" aria-label="Rezervasyon oluştur" onMouseDown={e=>e.stopPropagation()}>
    <div className="cp-dialog-head"><div className="cp-eyebrow">ALPINEOS EXPERIENCE</div><h2>{type==='rental'?'Ekipman rezervasyonu':type==='stay'?'Konaklama rezervasyonu':'Etkinlik kaydı'}</h2><p>{String(item.name)}</p></div><button className="cp-close" onClick={onClose} aria-label="Kapat"><X size={20}/></button>
    <form onSubmit={submit} className="cp-dialog-form">
      {type==='activity'?<div className="cp-confirm"><CalendarDays size={22}/><div><b>{dateTime(item.startsAt)}</b><span>{String(item.instructor)} · {amount(item.price)} / kişi</span></div></div>:<div className="cp-form-grid">
        <label>{type==='stay'?'Giriş tarihi':'Teslim alma'}<input required type={type==='stay'?'date':'datetime-local'} value={start} onChange={e=>setStart(e.target.value)}/></label>
        <label>{type==='stay'?'Çıkış tarihi':'İade zamanı'}<input required type={type==='stay'?'date':'datetime-local'} value={end} onChange={e=>setEnd(e.target.value)}/></label>
        {type==='stay'&&<label>Misafir sayısı<select value={guests} onChange={e=>setGuests(Number(e.target.value))}>{Array.from({length:Number(item.capacity)},(_,i)=><option key={i+1} value={i+1}>{i+1} kişi</option>)}</select></label>}
      </div>}
      {type!=='activity'&&<><div className="cp-price-summary"><span>Tahmini toplam · {days} {type==='stay'?'gece':'gün'}</span><strong>{amount(days*rate)}</strong></div><p className={`cp-availability ${availability?.available?'available':'unavailable'}`}>{availability?availability.available?'✓ Seçilen tarihler müsait':availability.reason:'Tarih uygunluğu kontrol ediliyor…'}</p></>}
      <p className="cp-dialog-note"><ShieldCheck size={15}/> Rezervasyon talebi anında işletmenin yönetim sistemine kaydedilir. Bu işlem online ödeme değildir.</p>
      {error&&<div className="cp-alert error">{error}</div>}
      <div className="cp-dialog-actions"><button type="button" className="cp-btn cp-btn-light" onClick={onClose}>Vazgeç</button><button type="submit" className="cp-btn cp-btn-dark" disabled={saving|| (type!=='activity'&&(days<=0||availability?.available!==true))}>{saving?'Kaydediliyor...':'Rezervasyonu onayla'}<ArrowRight size={17}/></button></div>
    </form>
  </div></div>;
}

function BookingList({rentals,stays,enrollments,onCancel}:{rentals:Item[];stays:Item[];enrollments:Item[];onCancel:(kind:'rental'|'stay'|'activity',id:string)=>void}){
  const [filter,setFilter]=useState<'all'|'rental'|'stay'|'activity'>('all');
  // Bütün rezervasyon türlerini aynı görüntüleme modeline dönüştürüyoruz.
  // Böylece farklı API yanıtlarının alanları TypeScript açısından güvenli kullanılır.
  type BookingRow = {
    kind: 'rental' | 'stay' | 'activity';
    id: string;
    primary: string;
    secondary: string;
    date: string;
    status: string;
    total: number;
    activityId?: string;
  };
  const all: BookingRow[] = [
    ...rentals.map(x => ({
      kind: 'rental' as const,
      id: x.id,
      primary: String(x.equipment ?? ''),
      secondary: String(x.assetCode ?? ''),
      date: String(x.startAt ?? ''),
      status: String(x.status ?? ''),
      total: Number(x.total ?? 0)
    })),
    ...stays.map(x => ({
      kind: 'stay' as const,
      id: x.id,
      primary: String(x.room ?? ''),
      secondary: String(x.roomCode ?? ''),
      date: String(x.startAt ?? ''),
      status: String(x.status ?? ''),
      total: Number(x.total ?? 0)
    })),
    ...enrollments.map(x => ({
      kind: 'activity' as const,
      id: x.id,
      activityId: String(x.activityId ?? ''),
      primary: String(x.activity ?? ''),
      secondary: String(x.instructor ?? ''),
      date: String(x.startsAt ?? ''),
      status: String(x.status ?? ''),
      total: Number(x.price ?? 0)
    }))
  ].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  const visible=all.filter(x=>filter==='all'||x.kind===filter);
  return <><div className="cp-seg">{([['all','Tümü'],['rental','Ekipman'],['stay','Konaklama'],['activity','Ders ve etkinlik']] as const).map(([key,name])=><button className={filter===key?'active':''} onClick={()=>setFilter(key)} key={key}>{name}</button>)}</div>
    {visible.length===0?<Empty text="Henüz bu kategoride rezervasyonunuz yok."/>:<div className="cp-bookings-list">{visible.map(b=><div className="cp-booking-row" key={`${b.kind}-${b.id}`}>
      <div className="cp-booking-icon">{b.kind==='rental'?<Package/>:b.kind==='stay'?<BedDouble/>:<GraduationCap/>}</div><div className="cp-booking-body"><span>{b.kind==='rental'?'EKİPMAN KİRALAMA':b.kind==='stay'?'KONAKLAMA':'DERS / ETKİNLİK'}</span><h3>{String(b.primary)}</h3><p>{String(b.secondary)} · {dateTime(b.date)}</p></div>
      <div className="cp-booking-right"><span className={`cp-status ${String(b.status)==='Cancelled'?'cancel':String(b.status)==='Reserved'?'pending':'success'}`}>{label(b.status)}</span><b>{amount(b.total)}</b>
        {String(b.status)===(b.kind==='activity'?'Enrolled':'Reserved')&&<button className="cp-text-action" onClick={()=>onCancel(b.kind,b.kind==='activity'?(b.activityId ?? b.id):b.id)}>İptal et</button>}</div></div>)}</div>}
  </>;
}
function Empty({text}:{text:string}){return <div className="cp-empty"><Snowflake size={35}/><h3>Burada henüz bir şey yok</h3><p>{text}</p></div>;}

export default function CustomerPortal({person,onLogout}:{person:Person;onLogout:()=>void}){
  const [page,setPage]=useState<Panel>('home');const [catalog,setCatalog]=useState<Catalog>({equipment:[],rooms:[],slopes:[],activities:[]});
  const [rentals,setRentals]=useState<Item[]>([]);const [stays,setStays]=useState<Item[]>([]);const [enrollments,setEnrollments]=useState<Item[]>([]);
  const [payments,setPayments]=useState<Item[]>([]);const [profile,setProfile]=useState<Profile|null>(null);const [search,setSearch]=useState('');
  const [booking,setBooking]=useState<Booking|null>(null);const [mobileNav,setMobileNav]=useState(false);
  const [busy,setBusy]=useState(true);const [error,setError]=useState('');const [notice,setNotice]=useState('');
  const [editing,setEditing]=useState(false);const [displayName,setDisplayName]=useState('');const [phone,setPhone]=useState('');
  const refresh=useCallback(async()=>{
    const [cat,r,s,e,p,u]=await Promise.all([
      request<Catalog>('/customer/catalog'),request<Item[]>('/customer/rentals'),request<Item[]>('/customer/stays'),
      request<Item[]>('/customer/enrollments'),request<Item[]>('/customer/payments'),request<Profile>('/customer/profile')]);
    setCatalog(cat);setRentals(r);setStays(s);setEnrollments(e);setPayments(p);setProfile(u);
    setDisplayName(u.name);setPhone(u.phone);
  },[]);
  useEffect(()=>{refresh().catch(e=>setError((e as Error).message)).finally(()=>setBusy(false));},[refresh]);
  const section=(value:Panel)=>{setPage(value);setMobileNav(false);setSearch('');setError('');window.scrollTo({top:0,behavior:'smooth'});};
  const booked=rentals.filter(x=>x.status==='Reserved').length+stays.filter(x=>x.status==='Reserved').length+enrollments.filter(x=>x.status==='Enrolled').length;
  const balances=useMemo(()=>{
    const due=[...rentals.filter(x=>x.status!=='Cancelled'),...stays.filter(x=>x.status!=='Cancelled')].reduce((sum,x)=>sum+Number(x.total||0),0);
    const paid=payments.reduce((sum,x)=>sum+(x.kind==='Refund'?-1:1)*Number(x.amount||0),0);
    return {due,paid,open:Math.max(0,due-paid)};
  },[rentals,stays,payments]);
  async function cancel(kind:'rental'|'stay'|'activity',id:string){
    if(!window.confirm('Bu kaydı iptal etmek istiyor musunuz?'))return;
    try{setError('');const endpoint=kind==='activity'?`/customer/activities/${id}/cancel`:`/customer/${kind==='rental'?'rentals':'stays'}/${id}/cancel`;
      await request(endpoint,'POST',kind==='activity'?undefined:{reason:'Müşteri tarafından portal üzerinden iptal edildi.'});
      await refresh();setNotice('İptal işlemi kaydedildi.');
    }catch(e){setError((e as Error).message);}
  }
  async function updateProfile(event:FormEvent){event.preventDefault();try{setError('');await request('/customer/profile','PUT',{name:displayName,phone});await refresh();setEditing(false);setNotice('Profil bilgileriniz güncellendi.');}catch(e){setError((e as Error).message);}}
  const filtered=(list:Item[])=>list.filter(x=>JSON.stringify(x).toLocaleLowerCase('tr').includes(search.toLocaleLowerCase('tr')));
  const activeName=leftNav.find(x=>x.id===page)?.name||'Keşfet';
  const actionCard=(kind:'rental'|'stay'|'activity',item:Item)=>setBooking({kind,item});
  return <div className="cp-shell">
    <header className="cp-header"><div className="cp-header-in"><button className="cp-logo" onClick={()=>section('home')}><span><MountainSnow size={25}/></span><b>ALPINE<span>OS</span></b><small>EXPERIENCE</small></button>
      <nav className={`cp-nav ${mobileNav?'open':''}`}>{leftNav.slice(0,7).map(x=><button key={x.id} onClick={()=>section(x.id)} className={page===x.id?'current':''}>{x.name}</button>)}</nav>
      <div className="cp-user-tools"><button className="cp-desktop-account" onClick={()=>section('profile')}><span className="cp-avatar">{(profile?.name||person.name)[0]?.toUpperCase()}</span><span><b>{(profile?.name||person.name).split(' ')[0]}</b><small>Misafir hesabı</small></span></button><button className="cp-icon" aria-label="Çıkış yap" onClick={async()=>{try{await logout();onLogout();}catch(e){setError((e as Error).message);}}}><LogOut size={19}/></button><button className="cp-mobile-menu" onClick={()=>setMobileNav(v=>!v)} aria-label="Menüyü aç"><Menu size={23}/></button></div>
    </div></header>
    {page==='home'?<div className="cp-hero"><div className="cp-hero-backdrop"><span className="cp-mtn p1"/><span className="cp-mtn p2"/><span className="cp-mtn p3"/></div><div className="cp-hero-content"><div className="cp-kicker"><Snowflake size={16}/> ALPINE EXPERIENCE · 2026/27 KIŞ SEZONU</div><h1>Dağ seni <em>bekliyor.</em></h1><p>Kayak ekipmanından konaklamaya, derslerden pistlere kadar tüm dağ deneyimin tek yerde.</p><div className="cp-hero-buttons"><button onClick={()=>section('equipment')} className="cp-btn cp-btn-white">Ekipmanları keşfet <ArrowRight size={17}/></button><button onClick={()=>section('rooms')} className="cp-btn cp-btn-outline">Konaklama seçenekleri</button></div></div><div className="cp-hero-badge"><Snowflake size={18}/><div><b>{catalog.slopes.filter(x=>x.status==='Open').length} açık pist</b><small>Güncel tesis durumu</small></div></div></div>:null}
    <main className="cp-main">
      {page!=='home'&&<div className="cp-page-title"><div><span className="cp-eyebrow">ALPINEOS / MİSAFİR PANELİ</span><h1>{activeName}</h1><p>{page==='bookings'?'Tüm rezervasyonlarını ve katılım kayıtlarını tek yerden yönet.':page==='payments'?'İşletme tarafından kaydedilmiş tahsilat ve iadelerin.':page==='profile'?'Hesap ve iletişim bilgilerin.':'Dağ deneyimini dilediğin gibi planla.'}</p></div><button className="cp-btn cp-btn-light" onClick={()=>section('home')}><ArrowLeft size={16}/> Keşfete dön</button></div>}
      {notice&&<div className="cp-alert success"><Check size={17}/>{notice}<button onClick={()=>setNotice('')}><X size={14}/></button></div>}
      {error&&<div className="cp-alert error">{error}<button onClick={()=>setError('')}><X size={14}/></button></div>}
      {busy?<div className="cp-loading"><Snowflake size={32}/>Deneyimin hazırlanıyor...</div>:<>
        {page==='home'&&<><div className="cp-welcome"><div><span className="cp-eyebrow">TEKRAR HOŞ GELDİN</span><h2>Merhaba, {profile?.name.split(' ')[0]} <span>👋</span></h2><p>Sıradaki maceran için her şey burada.</p></div><button className="cp-btn cp-btn-light" onClick={()=>section('bookings')}>Rezervasyonlarım <ChevronRight size={16}/></button></div>
          <div className="cp-stats"><div><span className="cp-stat-icon green"><CalendarCheck size={21}/></span><span>Aktif planlarım</span><b>{booked}</b><small>Rezervasyon ve kayıt</small></div><div><span className="cp-stat-icon blue"><MountainSnow size={21}/></span><span>Açık pistler</span><b>{catalog.slopes.filter(x=>x.status==='Open').length}<em> / {catalog.slopes.length}</em></b><small>Canlı pist bilgisi</small></div><div><span className="cp-stat-icon gold"><GraduationCap size={21}/></span><span>Yaklaşan dersler</span><b>{catalog.activities.length}</b><small>Katılıma açık etkinlik</small></div><div><span className="cp-stat-icon purple"><Wallet size={21}/></span><span>Hesaplanan bakiye</span><b className="money">{amount(balances.open)}</b><small>Ödeme gerektiren kayıtlar</small></div></div>
          <div className="cp-block-head"><div><span className="cp-eyebrow">KEŞFET & PLANLA</span><h2>Dağda seni neler bekliyor?</h2></div></div>
          <div className="cp-feature-grid"><button className="cp-feature equipment" onClick={()=>section('equipment')}><div className="cp-feature-art">🎿</div><span>KIŞ SPORLARI</span><h3>Ekipmanını seç</h3><p>{catalog.equipment.length} kiralanabilir ekipman · günlük ücretler</p><b>Keşfet <ArrowRight size={16}/></b></button><button className="cp-feature lodging" onClick={()=>section('rooms')}><div className="cp-feature-art">🏔️</div><span>KONAKLAMA</span><h3>Dağda bir gece</h3><p>{catalog.rooms.length} farklı oda ve dağ evi seçeneği</p><b>Odaları gör <ArrowRight size={16}/></b></button><button className="cp-feature activities" onClick={()=>section('lessons')}><div className="cp-feature-art">🏂</div><span>DENEYİMLER</span><h3>Yeni bir şey öğren</h3><p>Ders, yarışma ve etkinliklere katıl</p><b>Programı gör <ArrowRight size={16}/></b></button></div>
          <div className="cp-bottom-grid"><section className="cp-card cp-current"><div className="cp-block-head"><div><span className="cp-eyebrow">PİST RAPORU</span><h2>Dağ bugün nasıl?</h2></div><button onClick={()=>section('slopes')} className="cp-link">Tüm pistler <ArrowRight size={15}/></button></div>{catalog.slopes.slice(0,4).map(s=><div className="cp-slope-line" key={s.id}><span className={`cp-dot ${s.status==='Open'?'good':'bad'}`}/><b>{String(s.name)}</b><span>{label(s.difficulty)}</span><strong>{Number(s.snowDepthCm)} cm</strong></div>)}</section><section className="cp-card cp-current"><div className="cp-block-head"><div><span className="cp-eyebrow">YAKLAŞAN ETKİNLİKLER</span><h2>Takvimden seç</h2></div><button onClick={()=>section('lessons')} className="cp-link">Tümü <ArrowRight size={15}/></button></div>{catalog.activities.slice(0,3).map(a=><button key={a.id} className="cp-event-line" onClick={()=>actionCard('activity',a)}><span><b>{new Date(String(a.startsAt)).toLocaleDateString('tr-TR',{day:'2-digit',timeZone:'Europe/Istanbul'})}</b><small>{new Date(String(a.startsAt)).toLocaleDateString('tr-TR',{month:'short',timeZone:'Europe/Istanbul'})}</small></span><div><b>{String(a.name)}</b><small>{String(a.instructor)}</small></div><ChevronRight size={18}/></button>)}</section></div>
        </>}
        {['equipment','rooms','lessons','slopes'].includes(page)&&<>
          <div className="cp-search"><Search size={19}/><input aria-label="Katalogda ara" value={search} onChange={e=>setSearch(e.target.value)} placeholder="İsim, kategori veya özellik ara..."/><span>{filtered(page==='equipment'?catalog.equipment:page==='rooms'?catalog.rooms:page==='lessons'?catalog.activities:catalog.slopes).length} sonuç</span></div>
          {page==='equipment'&&<div className="cp-products">{filtered(catalog.equipment).map(e=><article className="cp-product" key={e.id}><div className="cp-product-art"><span>{iconFor(String(e.category))}</span><div className="cp-tag">{String(e.category)}</div></div><div className="cp-product-details"><small>{String(e.assetCode)} · {String(e.size)}</small><h3>{String(e.name)}</h3><div className="cp-product-meta"><Check size={14}/> Kullanıma uygun</div><div className="cp-product-foot"><div><b>{amount(e.dailyRate)}</b><small>/ gün</small></div><button className="cp-btn cp-btn-dark" onClick={()=>actionCard('rental',e)}>Kirala <ArrowRight size={15}/></button></div></div></article>)}</div>}
          {page==='rooms'&&<div className="cp-products cp-room-grid">{filtered(catalog.rooms).map((r,i)=><article className="cp-product cp-room" key={r.id}><div className={`cp-room-art color-${i%4}`}><BedDouble size={47}/><span>{String(r.code)}</span></div><div className="cp-product-details"><small><MapPin size={13}/> ALPINE MOUNTAIN · {String(r.code)}</small><h3>{String(r.name)}</h3><p>{String(r.description)}</p><div className="cp-product-meta"><CircleUserRound size={14}/> {Number(r.capacity)} misafir kapasitesi</div><div className="cp-product-foot"><div><b>{amount(r.nightlyRate)}</b><small>/ gece</small></div><button className="cp-btn cp-btn-dark" onClick={()=>actionCard('stay',r)}>Rezervasyon <ArrowRight size={15}/></button></div></div></article>)}</div>}
          {page==='lessons'&&<div className="cp-class-list">{filtered(catalog.activities).map(a=><div className="cp-class" key={a.id}><div className="cp-class-icon">{a.kind==='Lesson'?'🎿':a.kind==='Competition'?'🏆':'❄️'}</div><div className="cp-class-copy"><span>{label(a.kind)}</span><h3>{String(a.name)}</h3><p><Clock3 size={15}/> {dateTime(a.startsAt)} <i>·</i> {String(a.instructor)}</p><small>{Number(a.capacity)-Number(a.enrolled)} boş yer / {Number(a.capacity)} kontenjan</small></div><div className="cp-class-price"><strong>{amount(a.price)}</strong><button disabled={Number(a.enrolled)>=Number(a.capacity)} className="cp-btn cp-btn-dark" onClick={()=>actionCard('activity',a)}>Kayıt ol <ArrowRight size={15}/></button></div></div>)}</div>}
          {page==='slopes'&&<div className="cp-slope-grid">{filtered(catalog.slopes).map(s=><div className="cp-slope-card" key={s.id}><div className="cp-slope-head"><div className="cp-slope-img"><MountainSnow size={29}/></div><span className={`cp-status ${s.status==='Open'?'success':'cancel'}`}>{label(s.status)}</span></div><h3>{String(s.name)}</h3><p>{label(s.difficulty)} seviye</p><div className="cp-slope-details"><span><Snowflake size={16}/> {String(s.snowDepthCm)} cm kar</span><span><Wind size={16}/> {String(s.weather)}</span></div><small>Son güncelleme: {dateTime(s.updatedAt)}</small></div>)}</div>}
          {filtered(page==='equipment'?catalog.equipment:page==='rooms'?catalog.rooms:page==='lessons'?catalog.activities:catalog.slopes).length===0&&<Empty text="Aradığınız ölçütlerde bir seçenek bulamadık."/>}
        </>}
        {page==='bookings'&&<BookingList rentals={rentals} stays={stays} enrollments={enrollments} onCancel={cancel}/>}
        {page==='payments'&&<><div className="cp-payment-overview"><div><span>Toplam rezervasyon tutarı</span><b>{amount(balances.due)}</b></div><div><span>Net tahsilat</span><b>{amount(balances.paid)}</b></div><div><span>Hesaplanan kalan bakiye</span><b>{amount(balances.open)}</b></div></div><p className="cp-payment-hint"><ShieldCheck size={16}/> Bu sayfa ödeme geçmişinizi gösterir; çevrimiçi kartla ödeme alınmaz. Tahsilatlar tesis personeli tarafından kaydedilir.</p>{payments.length===0?<Empty text="Henüz hesabınızda ödeme hareketi bulunmuyor."/>:<div className="cp-payments-list">{payments.map(p=><div key={p.id} className="cp-payment-line"><div className="cp-payment-icon"><Wallet size={21}/></div><div><b>{label(p.kind)} · {p.referenceType==='Stay'?'Konaklama':'Kiralama'}</b><small>{dateTime(p.createdAt)} · {label(p.method)}</small></div><strong>{p.kind==='Refund'?'-':'+'}{amount(p.amount)}</strong></div>)}</div>}</>}
        {page==='profile'&&profile&&<div className="cp-profile-grid"><section className="cp-profile-banner"><div className="cp-big-avatar">{profile.name[0]?.toUpperCase()}</div><h2>{profile.name}</h2><p>{profile.email}</p><span><Check size={15}/> AlpineOS misafir hesabı</span></section><section className="cp-profile-form"><div className="cp-block-head"><div><span className="cp-eyebrow">HESAP BİLGİLERİ</span><h2>Kişisel bilgiler</h2></div>{!editing&&<button className="cp-link" onClick={()=>setEditing(true)}>Düzenle <ArrowRight size={15}/></button>}</div><form onSubmit={updateProfile}><label>Ad ve soyad<input disabled={!editing} value={displayName} onChange={e=>setDisplayName(e.target.value)} required minLength={2}/></label><label>E-posta<input disabled value={profile.email}/></label><label>Telefon<input disabled={!editing} value={phone} onChange={e=>setPhone(e.target.value)}/></label><label>Üyelik tarihi<input disabled value={dateText(profile.createdAt)}/></label>{editing&&<div className="cp-profile-actions"><button type="button" className="cp-btn cp-btn-light" onClick={()=>{setEditing(false);setDisplayName(profile.name);setPhone(profile.phone);}}>Vazgeç</button><button className="cp-btn cp-btn-dark">Kaydet <Check size={17}/></button></div>}</form></section></div>}
      </>}
      <footer className="cp-footer"><span><MountainSnow size={18}/> AlpineOS Experience · Kayak Merkezi</span><span>© {new Date().getFullYear()} · Güvenli misafir alanı</span><button onClick={()=>section('profile')}>Hesabım <ChevronRight size={15}/></button></footer>
    </main>
    {booking&&<BookDialog booking={booking} onClose={()=>setBooking(null)} onComplete={async()=>{await refresh();setBooking(null);setPage('bookings');setNotice('Rezervasyonunuz başarıyla oluşturuldu.');}}/>}
  </div>;
}

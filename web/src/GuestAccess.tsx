import { useState } from 'react';
import type { FormEvent } from 'react';
import { ArrowLeft, ArrowRight, CheckCircle2, MountainSnow, ShieldCheck, Snowflake } from 'lucide-react';
import { refreshCsrf, request } from './api';
import type { Person } from './api';

/** Guest sign-in/registration never uses a staff role or accepts an arbitrary customer id. */
export default function GuestAccess({onSuccess,onStaff}:{onSuccess:(person:Person)=>void;onStaff:()=>void}){
  const [register,setRegister]=useState(false);const [name,setName]=useState('');const [phone,setPhone]=useState('');
  const [email,setEmail]=useState('');const [password,setPassword]=useState('');const [error,setError]=useState('');const [busy,setBusy]=useState(false);
  async function submit(e:FormEvent){e.preventDefault();setError('');setBusy(true);
    try{const me=await request<Person>(register?'/customer/register':'/customer/login','POST',register?{name,phone,email,password}:{email,password});
      await refreshCsrf();onSuccess(me);
    }catch(err){setError((err as Error).message);}finally{setBusy(false);}
  }
  return <div className="login-screen"><div className="login-left"><div className="brand light"><span className="brand-symbol"><MountainSnow size={26}/></span><span><b>ALPINE<span className="brand-accent">OS</span></b><small>RESORT EXPERIENCE</small></span></div>
      <div className="mountain-drawing"><div className="sky-glow"/><div className="mountain-one"/><div className="mountain-two"/><div className="mountain-three"/></div>
      <div className="login-pitch"><span className="small-caps"><Snowflake size={16}/> ALPINEOS GUEST EXPERIENCE</span><h1>Dağda unutulmaz bir <em>deneyim başlıyor.</em></h1><p>Kayak ekipmanını seç, konaklama ayarla, etkinliklere katıl ve tüm rezervasyonlarını tek hesapla yönet.</p></div>
      <span className="login-copyright">© {new Date().getFullYear()} AlpineOS · Misafir Portalı</span></div>
    <div className="login-right"><div className="login-card"><div className="mobile-brand"><MountainSnow/> AlpineOS</div>
      <span className="login-tag"><ShieldCheck size={15}/> Güvenli misafir alanı</span>
      <h2>{register?'Maceraya katıl.':'Dağa hoş geldin.'}</h2><p>{register?'Birkaç bilgiyle misafir hesabını oluştur.':'Rezervasyonlarını yönetmek ve dağ deneyimini planlamak için giriş yap.'}</p>
      <div className="guest-switch"><button type="button" className={!register?'selected':''} onClick={()=>{setRegister(false);setError('');}}>Giriş yap</button><button type="button" className={register?'selected':''} onClick={()=>{setRegister(true);setError('');}}>Yeni hesap</button></div>
      <form onSubmit={submit}>{register&&<><label>Ad soyad<input required maxLength={120} minLength={2} value={name} onChange={e=>setName(e.target.value)} placeholder="Adınız Soyadınız" autoComplete="name"/></label><label>Telefon (isteğe bağlı)<input maxLength={35} value={phone} onChange={e=>setPhone(e.target.value)} placeholder="05xx xxx xx xx" autoComplete="tel"/></label></>}
        <label>E-posta adresi<input required type="email" maxLength={160} placeholder="ornek@eposta.com" value={email} onChange={e=>setEmail(e.target.value)} autoComplete="email"/></label>
        <label>Parola<input required type="password" minLength={register?12:undefined} maxLength={128} value={password} onChange={e=>setPassword(e.target.value)} autoComplete={register?'new-password':'current-password'} placeholder={register?'En az 12 karakter':'Parolanız'}/></label>
        {error&&<div className="message error">{error}</div>}
        <button className="btn btn-primary login-button" disabled={busy}>{busy?'İşlem yapılıyor...':register?'Ücretsiz hesap oluştur':'Misafir paneline giriş'}<ArrowRight size={18}/></button>
      </form><p className="guest-security"><CheckCircle2 size={16}/> Rezervasyonlarınız yalnızca hesabınıza görünür. Kart veya ödeme bilgisi toplanmaz.</p>
      <button className="guest-back" onClick={onStaff}><ArrowLeft size={16}/> Yönetici / personel girişine dön</button>
    </div></div>
  </div>;
}

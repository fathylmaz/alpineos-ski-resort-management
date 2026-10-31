using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Models;
using SkiResort.Api.Services;

namespace SkiResort.Api.Data;

/// <summary>Opt-in, additive, repeatable fictional demo data. NEVER seeds customer passwords.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ResortDb db, IPasswordHasher<StaffAccount> hasher, IConfiguration config)
    {
        if (!await db.Accounts.AnyAsync())
        {
            var email = config["ADMIN_EMAIL"];
            var password = config["ADMIN_PASSWORD"];
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || string.IsNullOrWhiteSpace(password) ||
                password.Length < 12 || password.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
                password.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Set a real ADMIN_EMAIL and ADMIN_PASSWORD before initial startup.");
            var admin = new StaffAccount { Email=email.Trim().ToLowerInvariant(),DisplayName="Sistem Yöneticisi",Role="Admin" };
            admin.PasswordHash=hasher.HashPassword(admin,password);
            db.Accounts.Add(admin);await db.SaveChangesAsync();
        }
        if (!bool.TryParse(config["SEED_DEMO_DATA"],out var demo) || !demo) return;

        string[] departmentNames = ["Kayak Eğitimi", "Resepsiyon", "Ekipman ve Kiralama", "Pist Operasyonları",
            "Konaklama", "Muhasebe ve Finans", "Misafir İlişkileri", "Teknik Bakım", "Etkinlik Yönetimi"];
        foreach (var name in departmentNames)
            if (!await db.Departments.AnyAsync(x=>x.Name==name)) db.Departments.Add(new Department{Name=name});
        await db.SaveChangesAsync();
        var depts = await db.Departments.ToDictionaryAsync(x=>x.Name,x=>x.Id);

        var staff = new (string Name,string Position,string Department)[] {
            ("Ece Demir", "Kıdemli Kayak Eğitmeni", "Kayak Eğitimi"), ("Baran Aksoy", "Snowboard Eğitmeni", "Kayak Eğitimi"),
            ("Merve Öz", "Resepsiyon Şefi", "Resepsiyon"), ("Tolga Yıldırım", "Resepsiyon Görevlisi", "Resepsiyon"),
            ("Duru Yalçın", "Ekipman Uzmanı", "Ekipman ve Kiralama"), ("Berk Kılıç", "Kiralama Sorumlusu", "Ekipman ve Kiralama"),
            ("Sarp Ergin", "Pist Güvenlik Sorumlusu", "Pist Operasyonları"), ("Canan Bilgin", "Pist Teknisyeni", "Pist Operasyonları"),
            ("Nil Kara", "Konaklama Yöneticisi", "Konaklama"), ("Nehir Atay", "Kat Hizmetleri Şefi", "Konaklama"),
            ("Ozan Kurt", "Muhasebe Uzmanı", "Muhasebe ve Finans"), ("Elif Çelik", "Misafir Deneyimi Uzmanı", "Misafir İlişkileri"),
            ("Hakan Sezer", "Bakım Teknisyeni", "Teknik Bakım"), ("İdil Eren", "Etkinlik Koordinatörü", "Etkinlik Yönetimi"),
            ("Arda Yüksel", "Kayak Eğitmeni", "Kayak Eğitimi"), ("Pelin Şen", "Müşteri İlişkileri Uzmanı", "Misafir İlişkileri")
        };
        for (var i=0;i<staff.Length;i++)
        {
            var person=staff[i];var email=$"demo.personel{i+1:00}@example.test";
            if(!await db.Employees.AnyAsync(x=>x.Email==email)) db.Employees.Add(new Employee {
                FullName=person.Name,Position=person.Position,DepartmentId=depts[person.Department],Email=email});
        }

        var names = new[] {"Ayşe Kaya","Deniz Arslan","Mert Şahin","Cemre Aydın","Aslı Koç","Kerem Yılmaz",
            "İrem Arı","Ömer Özkan","Buse Korkmaz","Ege Türkmen","Zeynep Doğan","Emir Demirci",
            "Sena Kaplan","Yiğit Polat","Selin Güneş","Onur Keskin","Cansu Öz","Alper Güven"};
        for (var i=0;i<names.Length;i++)
        {
            var email=$"demo.misafir{i+1:00}@example.test";
            if(!await db.Customers.AnyAsync(x=>x.Email==email)) db.Customers.Add(new Customer {
                FullName=names[i],Email=email,Phone=$"0500 000 {i+1:00} {i+1:00}",Notes="Kurgusal demo müşteri; portal hesabı yok."});
        }
        await db.SaveChangesAsync();

        var catalog = new (string Code,string Name,string Category,string Size,decimal Rate)[] {
            ("SKI-1001","All Mountain Explorer", "Kayak","165 cm",650), ("SKI-1002","Alpine Pro Carve","Kayak","175 cm",850),
            ("SKI-1003","Junior Discovery","Kayak","135 cm",410), ("SKI-1004","Freeride Powder","Kayak","182 cm",990),
            ("SKI-1005","All Mountain Explorer","Kayak","155 cm",650), ("SKI-1006","Touring Light","Kayak","170 cm",890),
            ("SKI-1007","Race Slalom","Kayak","160 cm",1100), ("SKI-1008","Beginner Comfort","Kayak","150 cm",490),
            ("SKI-1009","Beginner Comfort","Kayak","160 cm",490), ("SKI-1010","Junior Discovery","Kayak","145 cm",410),
            ("SKI-1011","All Mountain Explorer","Kayak","170 cm",650), ("SKI-1012","Alpine Pro Carve","Kayak","180 cm",850),
            ("BOOT-2001","Alpine Fit Bot","Bot","42",320), ("BOOT-2002","Alpine Fit Bot","Bot","40",320),
            ("BOOT-2003","Junior Easy Bot","Bot","35",220), ("BOOT-2004","Performance Flex","Bot","44",390),
            ("BOOT-2005","Performance Flex","Bot","43",390), ("BOOT-2006","Alpine Fit Bot","Bot","38",320),
            ("BOOT-2007","Junior Easy Bot","Bot","37",220), ("BOOT-2008","Alpine Fit Bot","Bot","41",320),
            ("HELM-3001","PeakSafe Kask","Kask","M",160), ("HELM-3002","PeakSafe Kask","Kask","L",160),
            ("HELM-3003","Junior Protect","Kask","S",130), ("HELM-3004","PeakSafe Kask","Kask","S",160),
            ("HELM-3005","PeakSafe Kask","Kask","XL",160), ("HELM-3006","Junior Protect","Kask","XS",130),
            ("SNOW-4001","Freestyle Twin Snowboard","Snowboard","155 cm",820), ("SNOW-4002","Cruiser Snowboard","Snowboard","160 cm",690),
            ("SNOW-4003","Junior Snowboard","Snowboard","125 cm",450), ("SNOW-4004","Powder Directional","Snowboard","165 cm",920),
            ("SNOW-4005","Cruiser Snowboard","Snowboard","150 cm",690), ("SNOW-4006","Freestyle Twin Snowboard","Snowboard","158 cm",820),
            ("POLE-5001","Alüminyum Baton","Baton","115 cm",110), ("POLE-5002","Alüminyum Baton","Baton","120 cm",110),
            ("POLE-5003","Junior Baton","Baton","95 cm",90), ("POLE-5004","Karbon Baton","Baton","125 cm",170),
            ("POLE-5005","Alüminyum Baton","Baton","110 cm",110), ("POLE-5006","Junior Baton","Baton","100 cm",90),
            ("GOGGLE-6001","Snow Vision Gözlük","Gözlük","M",180), ("GOGGLE-6002","Snow Vision Gözlük","Gözlük","L",180),
            ("GOGGLE-6003","Storm Lens Gözlük","Gözlük","L",260), ("GOGGLE-6004","Junior Clear Gözlük","Gözlük","S",140),
            ("ARM-7001","Sırt Koruma Yeleği","Koruma","M",250), ("ARM-7002","Sırt Koruma Yeleği","Koruma","L",250),
            ("ARM-7003","Dizlik Seti","Koruma","M",140), ("ARM-7004","Dizlik Seti","Koruma","L",140),
            ("JKT-8001","Termal Kayak Montu","Giyim","M",360), ("JKT-8002","Termal Kayak Montu","Giyim","L",360)
        };
        foreach(var (code,name,category,size,rate) in catalog)
            if(!await db.Equipment.AnyAsync(x=>x.AssetCode==code)) db.Equipment.Add(new Equipment{
                AssetCode=code,Name=name,Category=category,Size=size,DailyRate=rate});

        await db.SaveChangesAsync();
        // Sample maintenance cards on assets not used by the prefilled demo bookings.
        foreach(var (code,description,cost) in new (string Code,string Description,decimal Cost)[]{
            ("BOOT-2003","Taban ve kilit kontrolü",250),("ARM-7003","Koruma pedleri değişimi",180),
            ("SNOW-4004","Kenar bileme ve wax bakımı",310),("SKI-1012","Bağlama güvenlik kontrolü",220)})
        {
            var asset=await db.Equipment.FirstOrDefaultAsync(x=>x.AssetCode==code);
            if(asset==null || await db.Maintenance.AnyAsync(x=>x.EquipmentId==asset.Id))continue;
            if(await db.Rentals.AnyAsync(x=>x.EquipmentId==asset.Id && x.Status=="CheckedOut"))continue;
            db.Maintenance.Add(new Maintenance{EquipmentId=asset.Id,Description=description,Cost=cost});
            asset.Status="Maintenance";
        }
        await db.SaveChangesAsync();

        var rooms = new (string Code,string Name,string Description,int Capacity,decimal Price)[] {
            ("ALP-101","Dağ Manzaralı Standart","Çift kişilik yatak · vadi manzarası · kahvaltı",2,2900),
            ("ALP-102","Dağ Manzaralı Standart","İki tek kişilik yatak · balkon",2,2900),
            ("ALP-103","Ekonomik Twin","İki tek kişilik yatak · ekonomik konaklama",2,2200),
            ("ALP-104","Ekonomik Twin","İki tek kişilik yatak · pist erişimi",2,2200),
            ("ALP-105","Superior Balkon","Geniş balkon · şömine · kahvaltı",2,3500),
            ("ALP-106","Superior Balkon","Dağ manzarası · şömine",2,3500),
            ("ALP-201","Aile Süiti","İki yatak odası · oturma alanı",4,5200),
            ("ALP-202","Aile Süiti","Çocuk alanı · iki yatak odası",4,5200),
            ("ALP-203","Aile Deluxe","Dağ manzarası · çocuk dostu",5,6400),
            ("ALP-204","Aile Deluxe","Geniş balkon · kahvaltı",5,6400),
            ("ALP-205","Premium Süit","Jakuzi · panoramik vadi manzarası",3,7800),
            ("ALP-206","Premium Süit","Şömine · oturma alanı",3,7800),
            ("CHA-301","Ahşap Dağ Evi","Müstakil dağ evi · özel veranda",4,8900),
            ("CHA-302","Ahşap Dağ Evi","Şömineli müstakil dağ evi",4,8900),
            ("CHA-303","Panorama Chalet","Özel teras · şömine · iki yatak odası",6,12400),
            ("CHA-304","Panorama Chalet","Geniş aile / grup konaklaması",6,12400),
            ("ALP-301","Çatı Katı Suit","Şehirden uzak sessiz konaklama",2,4800),
            ("ALP-302","Çatı Katı Suit","Panoramik pencereler · çift kişilik",2,4800)
        };
        foreach(var (code,name,description,capacity,price) in rooms)
            if(!await db.Rooms.AnyAsync(x=>x.Code==code)) db.Rooms.Add(new Room{
                Code=code,Name=name,Description=description,Capacity=capacity,NightlyRate=price});

        var slopes = new (string Name,string Difficulty,string Status,int Snow,string Weather)[] {
            ("Çamlık","Easy","Open",85,"Açık"),("Orman Yolu","Medium","Open",94,"Parçalı bulutlu"),
            ("Zirve","Hard","Maintenance",110,"Rüzgarlı"),("Beyaz Vadi","Easy","Open",78,"Açık"),
            ("Kartal Pisti","Hard","Open",125,"Hafif kar"),("Gümüş Hat","Medium","Open",99,"Açık"),
            ("Kuzey Yamaç","Hard","Closed",118,"Tipi"),("Çocuk Parkuru","Easy","Open",65,"Açık"),
            ("Ayışığı Rotası","Medium","Closed",91,"Akşam seansı kapalı"),("Kristal Geçit","Medium","Open",102,"Parçalı bulutlu"),
            ("Buzul İnişi","Hard","Open",135,"Bulutlu"),("Ladin Yolu","Easy","Open",82,"Açık")
        };
        foreach(var (name,difficulty,status,snow,weather) in slopes)
            if(!await db.Slopes.AnyAsync(x=>x.Name==name)) db.Slopes.Add(new Slope{
                Name=name,Difficulty=difficulty,Status=status,SnowDepthCm=snow,Weather=weather});
        await db.SaveChangesAsync();

        var start=DateTime.UtcNow.Date.AddDays(3).AddHours(10);
        var events = new (string Name,string Kind,string Instructor,int Day,int Hour,int Duration,int Capacity,decimal Price)[]{
            ("Kayak Temelleri","Lesson","Ece Demir",0,10,2,8,1200),
            ("Snowboard Başlangıç","Lesson","Baran Aksoy",0,14,2,8,1350),
            ("Çocuklar İçin İlk Kayak","Lesson","Arda Yüksel",1,10,2,10,950),
            ("Orta Seviye Carving","Lesson","Ece Demir",1,13,2,7,1550),
            ("Zirve Kayak Yarışması","Competition","Etkinlik Ekibi",4,10,4,25,0),
            ("Freestyle Atölyesi","Lesson","Baran Aksoy",2,13,2,8,1750),
            ("Gece Kayak Deneyimi","Event","İdil Eren",5,18,2,30,850),
            ("İleri Seviye Pist Tekniği","Lesson","Ece Demir",3,10,3,5,2100),
            ("Aile Kayak Eğitimi","Lesson","Arda Yüksel",4,13,2,12,1250),
            ("Kar Güvenliği Semineri","Event","Sarp Ergin",2,16,1,40,0),
            ("Gençler Slalom Kupası","Competition","Etkinlik Ekibi",6,11,4,30,150),
            ("Snowboard Freeride","Lesson","Baran Aksoy",5,10,2,6,1950),
            ("Yeni Başlayanlar Pratiği","Lesson","Arda Yüksel",6,14,2,10,1050),
            ("Gün Doğumu Kayak Turu","Event","İdil Eren",7,8,2,18,650),
            ("Pist Rehberliği","Lesson","Ece Demir",8,10,2,8,1350),
            ("Kış Festivali","Event","Etkinlik Ekibi",9,12,5,120,0)
        };
        foreach(var (name,kind,instructor,day,hour,duration,capacity,price) in events)
            if(!await db.Activities.AnyAsync(x=>x.Name==name))db.Activities.Add(new Activity{
                Name=name,Kind=kind,Instructor=instructor,StartsAt=start.Date.AddDays(day).AddHours(hour),
                EndsAt=start.Date.AddDays(day).AddHours(hour+duration),Capacity=capacity,Price=price});
        await db.SaveChangesAsync();

        // Demo reservations are created only once, and only for dedicated fictitious contacts.
        var demoCustomers=await db.Customers.Where(x=>x.Email.StartsWith("demo.misafir") && x.Email.EndsWith("@example.test"))
            .OrderBy(x=>x.Email).Take(10).ToListAsync();
        var demoEquipment=await db.Equipment.Where(x=>x.AssetCode.StartsWith("SKI-")).OrderBy(x=>x.AssetCode).Take(5).ToListAsync();
        var demoRooms=await db.Rooms.Where(x=>x.Code.StartsWith("ALP-")).OrderBy(x=>x.Code).Take(5).ToListAsync();
        for (var i=0;i<Math.Min(5,Math.Min(demoCustomers.Count,demoEquipment.Count));i++)
        {
            var customer=demoCustomers[i];var e=demoEquipment[i];
            if(!await db.Rentals.AnyAsync(x=>x.CustomerId==customer.Id&&x.EquipmentId==e.Id))
            {
                var begins=DateTime.UtcNow.Date.AddDays(4+i).AddHours(9);
                var ends=begins.AddDays(2);
                // If someone has rented this physical asset for the period, preserve it.
                if(!await db.Rentals.AnyAsync(x=>x.EquipmentId==e.Id&&(x.Status=="Reserved"||x.Status=="CheckedOut")&&x.StartAt<ends&&begins<x.EndAt))
                    db.Rentals.Add(new Rental{CustomerId=customer.Id,EquipmentId=e.Id,StartAt=begins,EndAt=ends,
                        Total=Rules.RentalTotal(e.DailyRate,begins,ends)});
            }
        }
        for(var i=0;i<Math.Min(5,Math.Min(demoCustomers.Count,demoRooms.Count));i++)
        {
            var c=demoCustomers[i];var room=demoRooms[i];
            if(!await db.Stays.AnyAsync(x=>x.CustomerId==c.Id&&x.RoomId==room.Id))
            {
                var begins=DateTime.UtcNow.Date.AddDays(5+i);var ends=begins.AddDays(3);
                if(!await db.Stays.AnyAsync(x=>x.RoomId==room.Id&&(x.Status=="Reserved"||x.Status=="CheckedIn")&&x.StartAt<ends&&begins<x.EndAt))
                    db.Stays.Add(new Stay{CustomerId=c.Id,RoomId=room.Id,StartAt=begins,EndAt=ends,GuestCount=Math.Min(2,room.Capacity),
                        Total=Rules.StayTotal(room.NightlyRate,begins,ends)});
            }
        }
        await db.SaveChangesAsync();
        var exampleEvents=await db.Activities.Where(x=>x.Status=="Scheduled"&&x.StartsAt>DateTime.UtcNow)
            .OrderBy(x=>x.StartsAt).Take(4).ToListAsync();
        for(var i=0;i<Math.Min(demoCustomers.Count,8);i++)
        {
            if(exampleEvents.Count==0)break;
            var c=demoCustomers[i];var activity=exampleEvents[i%exampleEvents.Count];
            if(!await db.Enrollments.AnyAsync(x=>x.CustomerId==c.Id&&x.ActivityId==activity.Id) &&
                await db.Enrollments.CountAsync(x=>x.ActivityId==activity.Id&&x.Status=="Enrolled")<activity.Capacity)
                db.Enrollments.Add(new Enrollment{CustomerId=c.Id,ActivityId=activity.Id});
        }
        await db.SaveChangesAsync();
        // Historic demo ledger entries populate dashboard only; no real payments are processed.
        var seededRentals=await db.Rentals.Where(x=>x.Customer!.Email.StartsWith("demo.misafir") && x.Customer!.Email.EndsWith("@example.test"))
            .OrderBy(x=>x.CreatedAt).Take(4).ToListAsync();
        var seededStays=await db.Stays.Where(x=>x.Customer!.Email.StartsWith("demo.misafir") && x.Customer!.Email.EndsWith("@example.test"))
            .OrderBy(x=>x.CreatedAt).Take(4).ToListAsync();
        var entries=seededRentals.Select(x=>new {x.Id,x.CustomerId,ReferenceType="Rental",x.Total})
            .Concat(seededStays.Select(x=>new{x.Id,x.CustomerId,ReferenceType="Stay",x.Total})).ToList();
        for(var i=0;i<entries.Count;i++)
        {
            var entry=entries[i];
            if(!await db.Payments.AnyAsync(x=>x.ReferenceId==entry.Id && x.ReferenceType==entry.ReferenceType))
                db.Payments.Add(new Payment { CustomerId=entry.CustomerId,ReferenceId=entry.Id,
                    ReferenceType=entry.ReferenceType,Kind="Charge",Method="Cash",Amount=entry.Total/2,
                    ExternalReference=$"DEMO-{entry.ReferenceType}-{entry.Id:N}",CreatedAt=DateTime.UtcNow.Date.AddDays(-Math.Min(6,i)) });
        }
        await db.SaveChangesAsync();
    }
}

> ⚠️ **This plugin was developed entirely with AI assistance. I'm not sure it actually works — good luck to whoever tries to fix it.**
> ⚠️ **Bu eklenti tamamen yapay zeka yardımıyla geliştirilmiştir. Çalışırlığından emin değilim, düzeltmeye çalışacaklara bol şans.**

# FFMENU

A CounterStrikeSharp plugin for Counter-Strike 2 Jailbreak servers that manages timed Friendly Fire (FF) periods, gives Terrorists a weapon-selection menu before FF starts, and can freeze Terrorists inside a designated zone with an RGB boundary.

## Features
- Timed FF weapon menu: counts down, then opens a primary + secondary weapon selection menu for every alive Terrorist
- Simple FF toggle timer (opens FF after N seconds, no weapon menu)
- "FF + Freeze" (FFD) mode: counts down, closes FF, then freezes all Terrorists inside a chosen zone, killing (slaying) anyone caught outside the zone box
- In-game zone editor: mark P1/P2 corners by firing your weapon, then place a floating arrow marker at your current position
- Animated RGB laser-beam boundary box and a hovering, glowing arrow prop marking the freeze zone
- Live countdown HUD shown to all non-menu players (FF start, FF open, or FFD status with zone name)
- Automatic weapon-menu refresh every second while it's open, and auto weapon swap-out (removes the old primary/secondary before giving the new one)
- Zone data persisted to JSON, survives restarts
- Admin permission check (`@css/generic`) on every command

## Commands
- `css_ffmenu <seconds>` → Start the FF countdown and open the primary/secondary weapon menu for all alive Terrorists (1–30s)
- `css_ffmenu0` → Stop the FF menu timer and close any open weapon menus
- `css_ffz <seconds>` → Start a plain FF-open countdown (no weapon menu)
- `css_ffz0` → Stop the FFZ timer and force FF back off
- `css_ffd <seconds>` → Open a zone-select menu, then start the FF-close + freeze countdown for the chosen zone
- `css_ffd0` → Stop the FFD timer and unfreeze all Terrorists
- `css_ffdzoneayar` → Start zone setup (fire to mark P1, then P2)
- `css_ffdzoneayar0` → Cancel zone setup and clear temporary points
- `css_arrowadd` → Save your current position as the zone's arrow marker (requires P1/P2 already set)
- `css_ffdzonekaydet <name>` → Save the zone (requires P1, P2, and arrow all set)
- `css_ffdzonesil <name>` → Delete a saved zone

## Permissions
- `@css/generic` → required for every command in this plugin

## Configuration Files
Generated automatically in the plugin's module directory:
- `ff_zones.json` → saved freeze zones (box corners + arrow position)

## Requirements
- Metamod
- CounterStrikeSharp (API 1.0.*)
- CS2 Dedicated Server
- .NET 10.0

---

# FFMENU

CS2 Jailbreak sunucuları için, zamanlı Friendly Fire (FF) dönemlerini yöneten, FF başlamadan önce T takımına silah seçim menüsü sunan ve belirlenen bir bölgede T'leri RGB sınır çizgisiyle dondurabilen bir CounterStrikeSharp eklentisi.

## Özellikler
- Zamanlı FF silah menüsü: geri sayım yapar, ardından hayatta olan her T oyuncusu için birincil + ikincil silah seçim menüsü açar
- Basit FF açma zamanlayıcısı (N saniye sonra silah menüsü olmadan FF'yi açar)
- "FF + Dondur" (FFD) modu: geri sayım yapar, FF'yi kapatır, ardından seçilen bölgedeki tüm T'leri dondurur; bölge kutusu dışında kalanları slaylar
- Oyun içi bölge düzenleyici: ateş ederek P1/P2 köşelerini işaretle, ardından bulunduğun konuma yüzen bir ok işareti yerleştir
- Dondurma bölgesini gösteren animasyonlu RGB lazer ışın sınır kutusu ve havada süzülen parlayan ok prop'u
- Menüde olmayan tüm oyunculara gösterilen canlı geri sayım HUD'ı (FF başlangıcı, FF açılışı veya bölge adıyla FFD durumu)
- Açıkken her saniye otomatik silah menüsü yenileme ve otomatik silah değişimi (yeni silahı vermeden önce eski birincil/ikinciliği kaldırır)
- Bölge verileri JSON'a kaydedilir, restart sonrası korunur
- Her komutta yönetici yetki kontrolü (`@css/generic`)

## Komutlar
- `css_ffmenu <saniye>` → FF geri sayımını başlatır ve hayatta olan tüm T'ler için birincil/ikincil silah menüsünü açar (1–30s)
- `css_ffmenu0` → FF menü zamanlayıcısını durdurur ve açık silah menülerini kapatır
- `css_ffz <saniye>` → Silah menüsü olmadan sade bir FF açma geri sayımı başlatır
- `css_ffz0` → FFZ zamanlayıcısını durdurur ve FF'yi zorla kapatır
- `css_ffd <saniye>` → Bölge seçim menüsü açar, ardından seçilen bölge için FF-kapatma + dondurma geri sayımını başlatır
- `css_ffd0` → FFD zamanlayıcısını durdurur ve tüm T'lerin donunu çözer
- `css_ffdzoneayar` → Bölge ayarlamayı başlatır (ateş ederek P1'i, ardından P2'yi işaretle)
- `css_ffdzoneayar0` → Bölge ayarlamayı iptal eder ve geçici noktaları temizler
- `css_arrowadd` → Mevcut konumunu bölgenin ok işareti olarak kaydeder (önce P1/P2 ayarlanmış olmalı)
- `css_ffdzonekaydet <isim>` → Bölgeyi kaydeder (P1, P2 ve ok işaretinin hepsi ayarlanmış olmalı)
- `css_ffdzonesil <isim>` → Kayıtlı bir bölgeyi siler

## Yetkiler
- `@css/generic` → bu eklentideki tüm komutlar için gereklidir

## Yapılandırma Dosyaları
Eklentinin modül dizininde otomatik oluşturulur:
- `ff_zones.json` → kayıtlı dondurma bölgeleri (kutu köşeleri + ok konumu)

## Gereksinimler
- Metamod
- CounterStrikeSharp (API 1.0.*)
- CS2 Dedicated Server
- .NET 10.0

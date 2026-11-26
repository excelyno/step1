# Dokumentasi Grafis, Suara & Performa (MVP 6)

Dokumen ini menjelaskan tiga hal yang dikerjakan sekaligus:

1. **Aset baru** — cabai, sprite hama, bintang, ikon minigame, dan 18 efek suara.
2. **Game lebih hidup** — suara untuk hampir semua aksi, hama terlihat di ladang, umpan balik minigame.
3. **Optimasi rendering** — game tetap ringan walaupun grafisnya makin kaya.

---

## 1. Aset baru (Tools/generate_assets.py)

Semua gambar & suara dibuat dari kode. Jalankan ulang kapan saja (hasil deterministik):

```
python Tools/generate_assets.py
```

| Aset | Dipakai di |
|---|---|
| `crop_chili_0..4`, `produce_chili`, `seed_chili_seeds` | Tanaman **Cabai** (tanaman ke-6) |
| `pest_aphid`, `pest_snail`, `pest_crow`, `pest_mold` | Tanda hama di ladang + sel minigame "Tangkap Hama" |
| `lesson_leaf`, `lesson_bud` | Sel "jangan diklik" di minigame grid |
| `star_full`, `star_empty` | Hasil minigame + halaman Buku Tani |
| `Assets/SFX/*.wav` (18 file) | Efek suara (lihat daftar di bawah) |

## 2. Efek suara

Sistem: `Rendering/SoundPlayer.cs`. File WAV dimuat sekali saat game mulai,
diputar dengan `System.Media.SoundPlayer.Play()` (thread sendiri, tidak pernah
membekukan game). Suara yang hilang = diabaikan, game tetap jalan.

Akses dari mana saja lewat `IGameServices.PlaySound(nama)`.

| Suara | Kapan |
|---|---|
| `hoe` | Mencangkul |
| `water` | Menyiram / isi penyiram |
| `plant` | Menanam bibit |
| `harvest` | Panen masuk tas |
| `coin` | Bonus panen, koin air mancur, hadiah quest, ganti hari |
| `wood`, `stone` | Kapak, beliung |
| `door` | Pindah lokasi (pintu / warp) |
| `eat` | Makan |
| `ui_open`, `ui_close`, `select` | Buka/tutup menu, ganti tab, tanda "!" |
| `pop`, `good`, `wrong` | Minigame: tekan benar / salah |
| `win` | Minigame selesai bagus |
| `star` | (siap dipakai) bintang bertambah |
| `combo` | (siap dipakai) tekan beruntun benar |

`PlayVaried()` memutar dengan variasi kecil supaya suara aksi berulang (cangkul
20 petak) tidak terdengar seperti robot.

## 3. Minigame lebih hidup

| Perubahan | Efek terhadap pemain |
|---|---|
| Sprite hama & daun asli | Hama dikenali bentuknya (kutu, bekicot, burung, jamur) |
| Bintang sprite + muncul satu-satu | Layar hasil terasa dihadiahi |
| Layar bergetar pelan saat salah | Kesalahan terasa, tapi tidak menakutkan |
| Suara benar/salah/buka | Umpan balik tanpa harus membaca teks |
| Cabai: petik yang MERAH | Pelajaran baru: buah muda dibiarkan agar tumbuh lagi |

## 4. Optimasi rendering

Semua perubahan **tidak mengubah tampilan**, hanya cara menggambar:

| Optimasi | Sebelum | Sesudah |
|---|---|---|
| **ChunkCache** (`Rendering/ChunkCache.cs`) | ratusan DrawImage per frame (tile + tepi rumput/air) | 2–6 tempelan potongan 8×8 tile; digambar sekali lalu dipakai terus |
| Air beranimasi | ikut gambar tile | hanya barisan air/jembatan yang digambar ulang (`DrawAnimatedTiles`) |
| **Y-sort buffer** | urutan ratusan objek diurutkan tiap frame + alokasi daftar baru | diurutkan setiap frame ke buffer yang dipakai ulang (DrawOrder bergantung posisi pemain/NPC; cache berbasis daftar membuat pemain "ketimpa" bangunan) |
| **Banner cache** | bitmap spanduk dibuat tiap frame | dibuat sekali per teks (`Hud.BannerLayer`) |
| Partikel | 1 kuas baru per partikel per frame | kuas di-cache, alfa dibulatkan ke 16 tingkat |
| Sprite key tanah | string dibentuk tiap frame tiap petak | di-cache, `InvalidateSprite()` saat disiram/tetangga berubah |

Cara mengukur: tekan **F3** (FPS, jumlah objek). Bandingkan sebelum/sesudah.

## 5. Struktur file baru / berubah

| File | Isi |
|---|---|
| `Rendering/SoundPlayer.cs` | Baru. Pemutar WAV non-blok |
| `Rendering/ChunkCache.cs` | Baru. Cache potongan peta + invalidasi |
| `Rendering/BrushCache.cs` | Baru. Kuas/pena statis (siap dipakai UI lain) |
| `Tools/generate_assets.py` | +cabai, +hama, +bintang, +SFX |
| `Content/Data/crops.json` | +baris cabai |
| `Content/Data/lessons.json` | +minigame `chili_pick` |
| `Rendering/Renderer.cs` | lapisan tanah dari cache; Y-sort tiap frame (buffer dipakai ulang) |
| `Rendering/SpriteAssets.cs` | `DrawAnimatedTiles` |
| `Rendering/Particles.cs` | kuas di-cache |
| `UI/Hud.cs` | spanduk di-cache |
| `World/Objects/HoeDirt.cs` | hama pakai sprite; cache sprite key |
| `World/GameLocation.cs` | `ObjectsVersion` |
| `Items/Tool.cs`, `Seed.cs`, `Hoe.cs` | suara aksi; invalidasi petak tetangga |
| `Core/Game.cs` | `Sounds`, `ChunksFor`, `PlaySound` |

## 6. Cek manual

- [ ] `dotnet run` jalan; suara terdengar saat cangkul/siram/panen/buka menu.
- [ ] F3: FPS tetap tinggi saat ladang penuh tanaman dan hujan partikel.
- [ ] Cabai bisa dibeli (musim panas/gugur), ditanam, dipanen lewat minigame petik-merah.
- [ ] Hama di ladang terlihat sebagai gambar hama (bukan bintik), X membuka minigame.
- [ ] Bintang hasil minigame & Buku Tani memakai gambar bintang.
- [ ] Layar bergetar pelan ketika salah di minigame.
- [ ] `python Tools/generate_assets.py` bisa dijalankan ulang tanpa mengubah hasil.

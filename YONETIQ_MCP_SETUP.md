# YonetIQ — MCP SQL Server Kurulum + Schema Discovery Agent

**Tarih:** 25.03.2026 | **Amaç:** Claude Code, BKM veritabanını MCP üzerinden okur, şemayı keşfeder, sana sorar, schema_mapping.json yazar.

---

## BÖLÜM A — MCP KURULUM

### A.1 — Ön koşullar
```bash
node --version   # v18+ olmalı
npm --version
```

### A.2 — MCP Server Kurulumu
```bash
npm install -g @bilims/mcp-sqlserver
mcp-sqlserver --version
```

### A.3 — Claude Code'a Ekle

**Windows Integrated Security:**
```bash
claude mcp add bkm-db mcp-sqlserver \
  --env SQLSERVER_HOST="BKMSQL01" \
  --env SQLSERVER_DATABASE="BKMKitap" \
  --env SQLSERVER_WINDOWS_AUTH="true"
```

**SQL Auth:**
```bash
claude mcp add bkm-db mcp-sqlserver \
  --env SQLSERVER_HOST="BKMSQL01" \
  --env SQLSERVER_DATABASE="BKMKitap" \
  --env SQLSERVER_USER="yonetiq_readonly" \
  --env SQLSERVER_PASSWORD="<şifre>"
```

---

## BÖLÜM B — READ-ONLY KULLANICI

```sql
CREATE LOGIN yonetiq_readonly WITH PASSWORD = 'Ynt2026!Readonly';
CREATE USER  yonetiq_readonly FOR LOGIN yonetiq_readonly;
EXEC sp_addrolemember 'db_datareader', 'yonetiq_readonly';
GRANT VIEW DEFINITION TO yonetiq_readonly;
GRANT SELECT ON SCHEMA::dbo TO yonetiq_readonly;
```

---

## BÖLÜM C — SCHEMA DISCOVERY AGENT

9 adımlı keşif protokolü:
1. schema_mapping.json kontrol
2. Tablo keşfi (INFORMATION_SCHEMA)
3. Aday tespiti (satış, ürün, mağaza, müşteri)
4. Soru listesi (kritik + önemli)
5. Örnek veri doğrulama
6. Onay al
7. schema_mapping.json yaz
8. SemanticDefinitions bootstrap
9. Tamamlandı

---

## BÖLÜM D — SEMANTIC BOOTSTRAP SCRIPT

`scripts/semantic_bootstrap.ps1` — schema_mapping.json'dan SemanticDefinitions tablosunu doldurur.

---

## BÖLÜM G — ÇALIŞMA AKIŞI

```
Claude Code açılır
    ↓
schema_mapping.json var mı?
    ├── VAR → mapping'i yükle → development'a geç
    └── YOK → MCP ile keşif → sorular → onay → mapping yaz → bootstrap
```

MCP sadece onboarding için değil — tüm geliştirme süresince SQL doğrulama için açık kalır.

---

## KURULUM ÖZETİ

1. `npm install -g @bilims/mcp-sqlserver`
2. `claude mcp add bkm-db mcp-sqlserver --env SQLSERVER_HOST="..." ...`
3. Claude Code'u aç, schema_discovery_agent.md'yi ver

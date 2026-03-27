# MVP aplikacije (platform meta model)

V tej mapi so primeri aplikacij in extensiona za namestitev, deploy in testiranje.

## Datoteke

| Datoteka | Opis |
|----------|------|
| **extension-oseba.json** | Extension „Oseba“: entiteta Oseba z lastnostmi ime, priimek, naslov, email. Validacija: proti `defs/ExtensionDefinition.json`. Uporaba: vgradnja v `application.extensionDefinitions` ali objava v extension katalog. |
| **lovec.json** | Aplikacija **Lovec** (lovska društva): referenca na extension oseba + dodatek članska_stevilka, entiteta Lovišče, strani Lovci (list/edit), Lovišča (list/edit), navigacija. Root objekt z `application`. Validacija: proti `application-meta-model.schema.json` (iz mape `application/`). |
| **gradbisce.json** | Aplikacija **Gradbišče**: referenca na extension oseba, entitete Zaposlen (relacija na oseba.Oseba + številka_noge, številka_oblacila), Investitor (relacija na oseba.Oseba), Gradbišče (naziv, lokacija, investitor), strani in navigacija. Root objekt z `application`. Validacija: proti `application-meta-model.schema.json`. |
| **lovec.persistence.json** | Persistence definicija za **Lovec**: postgres, snake_case; entiteti oseba.Oseba (tabela oseba_oseba), Lovisce (lovisce); indeksi za članska_stevilka (unique), email, naziv. |
| **gradbisce.persistence.json** | Persistence definicija za **Gradbišče**: postgres, snake_case; entiteti oseba.Oseba (oseba_oseba), Zaposlen (zaposlen), Investitor (investitor), Gradbisce (gradbisce); indeksi po potrebi. |

## Validacija

- Iz mape **`application/`**: `lovec.json` in `gradbisce.json` validirata z root shemo (relativne `$ref` glede na `defs/` in `../../Common/`).
- Extension **oseba** validira kot instance `ExtensionDefinition` (vsebina `extension-oseba.json`).

## Namestitev in deploy

1. Extension **oseba** objavi v extension katalog (ali vključi v aplikacijo prek `extensionDefinitions`).
2. Aplikaciji **Lovec** in **Gradbišče** namestiš iz kataloga (ali uvoziš definicijo); obe referencirata extension `oseba` z `extensionReferences`.
3. Za tenant ustvariš okolje in deployaš izbrano aplikacijo (lifecycle: tenant application → release → deploy).

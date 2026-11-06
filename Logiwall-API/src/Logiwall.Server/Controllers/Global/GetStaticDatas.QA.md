# QA: GET /api/Common/GetStaticDatas

## Acceptance criteria

OriginCity (11) and DestinationCity (12) return internal cities with ETipax mappings. Include ProvinceTitle without changing Id/Title.
Other categories retain their existing filtering and response envelope. The endpoint remains public.

## Scenarios

- After migration, 678 distinct ETipax mappings exist in each category. Verify duplicate city names use province data and different codes.
- Search filters titles. An unmatched search returns an empty list. Old unmapped city IDs do not appear in new selections.
- Legacy order cities remain stored; migration does not rewrite their IDs. MyTipax and ETipax codes remain separate.
- No provider calls or secrets are involved. Check frontend city selection handles optional ProvinceTitle.
- Concurrent reads during reference catalogue sync remain stable; a failed catalogue sync cannot delete cities.

SQL integration coverage: ShipmentTests.Migration_imports_separate_etipax_city_codes.

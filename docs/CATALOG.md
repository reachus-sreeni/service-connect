# Configurable service catalog

The app contains **34 categories, 227 selectable service options and nine service families**. It maps all **69 named offerings in Urban Company's public India index** reviewed on October 27, 2026. City repetitions, premium tiers and partner-branded variants are consolidated into general service definitions. Gardening and Automotive Service remain additional user-requested categories.

This is a catalog of service-request types, usable in any area. It does not import Urban Company's city availability, product inventory, prices, ratings, guarantees, provider network or trademarked package branding. It does not duplicate every city-specific SKU or package combination. Consultations are request types; built-in video calling and product checkout are not implemented.

## Configuration

The single source is `backend/ServiceConnect.Api/Catalog/india-services.json`, embedded in the API build. It defines stable category/option IDs, names, descriptions, family names, Popular flags and a source-coverage map. Editing the file and rebuilding changes the available definitions without adding C# seed branches.

- Keep existing IDs and keys stable. Renaming/reordering a service must not create a new ID for a choice already referenced by requests.
- Use new unique GUIDs and keys for new categories/options. Names must be unique within their category.
- `groupName` controls the service-family filter. `isFeatured` controls the Popular shortlist; all categories remain reachable in All services, by family, and through search.
- Development startup applies migrations and synchronizes catalog definitions. SQL Server uses the explicit `--init-db` release step. Normal production startup does not mutate the catalog.
- Synchronization updates configuration-owned labels and adds missing rows. It does not delete categories/options or replace existing requests. Back up real data before schema/configuration changes.
- Location is provided on each request. The dataset has no India-only eligibility rule. Local providers still determine what work they can offer and the estimate currency remains the application's existing USD setting.
- Specialist skin/laser entries are service requests for appropriately qualified providers. The existing generic provider approval process is not a medical-license verification system. No treatment advice or automated clinical assessment is provided.

## Category inventory

| Service family | Category | Options |
| --- | --- | ---: |
| Appliance care | AC Service & Repair | 7 |
| Appliance care | Air Cooler Services | 2 |
| Appliance care | Air Purifier Services | 2 |
| Appliance care | Chimney Service & Repair | 4 |
| Appliance care | Geyser Service & Repair | 4 |
| Appliance care | Inverter Services | 4 |
| Appliance care | Laptop Repair | 2 |
| Appliance care | Microwave Repair | 3 |
| Appliance care | Refrigerator Service & Repair | 7 |
| Appliance care | Stove Service & Repair | 2 |
| Appliance care | Television Repair | 2 |
| Appliance care | Washing Machine Repair | 3 |
| Appliance care | Water Purifier Services | 5 |
| Automotive | Automotive Service | 5 |
| Beauty & grooming | Makeup & Occasion Styling | 6 |
| Beauty & grooming | Men’s & Kids’ Grooming | 10 |
| Beauty & grooming | Women’s Hair Services | 8 |
| Beauty & grooming | Women’s Salon | 12 |
| Cleaning & hygiene | Bathroom & Kitchen Cleaning | 8 |
| Cleaning & hygiene | Cleaning | 10 |
| Cleaning & hygiene | Disinfection Services | 3 |
| Cleaning & hygiene | Pest Control | 6 |
| Cleaning & hygiene | Upholstery & Furniture Cleaning | 11 |
| Garden & outdoor | Gardening | 5 |
| Home improvement | Furniture Assembly | 11 |
| Home improvement | Painting & Waterproofing | 10 |
| Home improvement | Smart Locks | 3 |
| Home improvement | Wall Decor & Panels | 10 |
| Home repairs | Carpentry | 15 |
| Home repairs | Electrical | 13 |
| Home repairs | Plumbing | 13 |
| Skin & laser services | Skin & Laser Services | 4 |
| Spa & wellness | Men’s Massage | 8 |
| Spa & wellness | Women’s Spa & Massage | 9 |

## Source coverage

Each named entry below maps to a configured category; the options under that category cover its service types. Partner names and premium labels are retained only in this audit, not presented as affiliations. The representative menu was retrieved for 68 entries. The microwave video-consultation detail page was unavailable (404/403), so that offering is supported by the public India index only; no unavailable menu contents are claimed.

Source index: https://www.urbancompany.com/

| India-index offering | Configured category | Evidence |
| --- | --- | --- |
| [Ac Service And Repair](https://www.urbancompany.com/bangalore-ac-service-repair) | AC Service & Repair | Index + representative service menu |
| [Painting & Waterproofing](https://www.urbancompany.com/vijayawada-house-painters) | Painting & Waterproofing | Index + representative service menu |
| [Statement Design Wall](https://www.urbancompany.com/bangalore-wall-makeover) | Wall Decor & Panels | Index + representative service menu |
| [Salon Prime For Kids & Men](https://www.urbancompany.com/bangalore-mens-grooming) | Men’s & Kids’ Grooming | Index + representative service menu |
| [Bathroom & Kitchen Cleaning](https://www.urbancompany.com/jamshedpur-professional-cleaning-services) | Bathroom & Kitchen Cleaning | Index + representative service menu |
| [Native Water Purifier](https://www.urbancompany.com/bangalore-ro-purchase) | Water Purifier Services | Index + representative service menu |
| [Refrigerator](https://www.urbancompany.com/bangalore-refrigerator-repair) | Refrigerator Service & Repair | Index + representative service menu |
| [Microwave](https://www.urbancompany.com/bangalore-microwave-repair) | Microwave Repair | Index + representative service menu |
| [Spa For Women](https://www.urbancompany.com/bangalore-spa-at-home) | Women’s Spa & Massage | Index + representative service menu |
| [Spa Luxe](https://www.urbancompany.com/bangalore-spa-luxe) | Women’s Spa & Massage | Index + representative service menu |
| [Stove Service & Repair](https://www.urbancompany.com/bangalore-gas-stove-repair) | Stove Service & Repair | Index + representative service menu |
| [Carpenters_amazon](https://www.urbancompany.com/ahmedabad-carpenters-amazon) | Carpentry | Index + representative service menu |
| [Carpenter](https://www.urbancompany.com/bangalore-carpenters-density) | Carpentry | Index + representative service menu |
| [Plumber](https://www.urbancompany.com/bangalore-plumbers-density) | Plumbing | Index + representative service menu |
| [Washing Machine](https://www.urbancompany.com/bangalore-washing-machine-repair) | Washing Machine Repair | Index + representative service menu |
| [Television Repair](https://www.urbancompany.com/bangalore-tv-repair) | Television Repair | Index + representative service menu |
| [Water Purifier](https://www.urbancompany.com/bangalore-ro-repair) | Water Purifier Services | Index + representative service menu |
| [Spa Ayurveda](https://www.urbancompany.com/bangalore-spa-ayurveda) | Women’s Spa & Massage | Index + representative service menu |
| [Furniture Assembly](https://www.urbancompany.com/bangalore-furniture-assembly) | Furniture Assembly | Index + representative service menu |
| [Native Smart Locks](https://www.urbancompany.com/bangalore-epc-stores-smarthome) | Smart Locks | Index + representative service menu |
| [Salon Royale](https://www.urbancompany.com/bangalore-mg-luxe) | Men’s & Kids’ Grooming | Index + representative service menu |
| [Electrician](https://www.urbancompany.com/bangalore-electrician-density) | Electrical | Index + representative service menu |
| [Full Home/ Book By Room Cleaning](https://www.urbancompany.com/bangalore-professional-home-cleaning) | Cleaning | Index + representative service menu |
| [Geyser Service & Repair](https://www.urbancompany.com/bangalore-geyser-reapir) | Geyser Service & Repair | Index + representative service menu |
| [Massage For Men](https://www.urbancompany.com/bangalore-massage-for-men) | Men’s Massage | Index + representative service menu |
| [Sofa & Carpet Cleaning](https://www.urbancompany.com/bangalore-professional-sofa-cleaning) | Upholstery & Furniture Cleaning | Index + representative service menu |
| [Hair Studio For Women](https://www.urbancompany.com/bangalore-women-hair-services) | Women’s Hair Services | Index + representative service menu |
| [Salon Luxe](https://www.urbancompany.com/bangalore-salon-luxe) | Women’s Salon | Index + representative service menu |
| [Salon Prime](https://www.urbancompany.com/bangalore-salon-at-home) | Women’s Salon | Index + representative service menu |
| [Salon Classic](https://www.urbancompany.com/patna-salon-basics) | Women’s Salon | Index + representative service menu |
| [Plumbers_amazon](https://www.urbancompany.com/patna-plumbers-amazon) | Plumbing | Index + representative service menu |
| [Talk To Expert](https://www.urbancompany.com/delhi-ncr-epc-stores-vc) | Smart Locks | Index + representative service menu |
| [Rooms/ Walls Painting](https://www.urbancompany.com/bangalore-painting-shp-fulfilment) | Painting & Waterproofing | Index + representative service menu |
| [Electricians_amazon](https://www.urbancompany.com/delhi-ncr-electricians-amazon) | Electrical | Index + representative service menu |
| [Refrigerator Video Consult](https://www.urbancompany.com/delhi-ncr-refrigerator-repair-vc) | Refrigerator Service & Repair | Index + representative service menu |
| [Ac_amazon](https://www.urbancompany.com/delhi-ncr-ac-amazon) | AC Service & Repair | Index + representative service menu |
| [Carpenters_flipkart](https://www.urbancompany.com/delhi-ncr-carpenters-flipkart) | Carpentry | Index + representative service menu |
| [Few Rooms & Walls Painting](https://www.urbancompany.com/bangalore-painting-shp-sku-survey) | Painting & Waterproofing | Index + representative service menu |
| [Wall Panels Installation](https://www.urbancompany.com/bangalore-painting-services) | Wall Decor & Panels | Index + representative service menu |
| [Laptop Repair](https://www.urbancompany.com/bangalore-laptop-repair) | Laptop Repair | Index + representative service menu |
| [Air Purifier](https://www.urbancompany.com/delhi-ncr-air-purifier) | Air Purifier Services | Index + representative service menu |
| [Ikea Furniture Assembly](https://www.urbancompany.com/bangalore-ikea-furniture-assembly) | Furniture Assembly | Index + representative service menu |
| [Chimney Repair](https://www.urbancompany.com/bangalore-chimney-and-hob-repair) | Chimney Service & Repair | Index + representative service menu |
| [Massage Therapy For Men Royale](https://www.urbancompany.com/bangalore-massage-for-men-luxe) | Men’s Massage | Index + representative service menu |
| [Ayurvedic Massage For Men](https://www.urbancompany.com/bangalore-mfm-ayurveda) | Men’s Massage | Index + representative service menu |
| [Makeup, Saree & Styling](https://www.urbancompany.com/bangalore-party-makeup-artist) | Makeup & Occasion Styling | Index + representative service menu |
| [Rooms/walls Painting](https://www.urbancompany.com/bangalore-painting-shp-sku) | Painting & Waterproofing | Index + representative service menu |
| [Pest Control Follow Up Service](https://www.urbancompany.com/delhi-ncr-pest-control-followup) | Pest Control | Index + representative service menu |
| [Kitchen Cleaning](https://www.urbancompany.com/delhi-ncr-professional-kitchen-cleaning) | Bathroom & Kitchen Cleaning | Index + representative service menu |
| [Bathroom Cleaning](https://www.urbancompany.com/delhi-ncr-professional-bathroom-cleaning) | Bathroom & Kitchen Cleaning | Index + representative service menu |
| [Disinfection Services](https://www.urbancompany.com/bangalore-home-disinfection) | Disinfection Services | Index + representative service menu |
| [Microwave Video Consult](https://www.urbancompany.com/chennai-microwave-repair-vc) | Microwave Repair | Index only; detail page unavailable |
| [Fan Installation](https://www.urbancompany.com/bangalore-fan-installation) | Electrical | Index + representative service menu |
| [Painting Consultation](https://www.urbancompany.com/hyderabad-painting-vc) | Painting & Waterproofing | Index + representative service menu |
| [Expert Counseling - Facials & Peels](https://www.urbancompany.com/hyderabad-free-dc-facials) | Skin & Laser Services | Index + representative service menu |
| [Ants & Bed Bugs Control](https://www.urbancompany.com/bangalore-bed-bugs-control) | Pest Control | Index + representative service menu |
| [Festival Lights Installation](https://www.urbancompany.com/bangalore-diwali-lights-decoration) | Electrical | Index + representative service menu |
| [Carpet Cleaning](https://www.urbancompany.com/mumbai-professional-carpet-cleaning) | Upholstery & Furniture Cleaning | Index + representative service menu |
| [Air Cooler Service & Repair](https://www.urbancompany.com/mumbai-air-cooler-repair) | Air Cooler Services | Index + representative service menu |
| [Rentomojo Refrigerator Repair](https://www.urbancompany.com/chandigarh-rentomojo-refrigerator-repair) | Refrigerator Service & Repair | Index + representative service menu |
| [Laser Doctor Consultation](https://www.urbancompany.com/bangalore-free-dc) | Skin & Laser Services | Index + representative service menu |
| [Instant Video Consultation](https://www.urbancompany.com/bangalore-skinclinic-instant-dc) | Skin & Laser Services | Index + representative service menu |
| [Weekly Bathroom Cleaning](https://www.urbancompany.com/bangalore-routines-bathroom-cleaning) | Bathroom & Kitchen Cleaning | Index + representative service menu |
| [Electrician (flood Support)](https://www.urbancompany.com/guntur-electrician-ap-flood) | Electrical | Index + representative service menu |
| [Plumber (flood Support)](https://www.urbancompany.com/guntur-plumber-ap-flood) | Plumbing | Index + representative service menu |
| [Water Purifier Video Consult](https://www.urbancompany.com/coimbatore-ro-repair-vc) | Water Purifier Services | Index + representative service menu |
| [Pest Control](https://www.urbancompany.com/indore-general-pest-control) | Pest Control | Index + representative service menu |
| [Inverter](https://www.urbancompany.com/lucknow-inverter-repair) | Inverter Services | Index + representative service menu |
| [Laser Hair Reduction](https://www.urbancompany.com/lucknow-skin-hair-consultation) | Skin & Laser Services | Index + representative service menu |

## Verification

Automated tests require every source entry to resolve to a configured category with options, verify all stable IDs, run synchronization twice without duplicate rows, preserve an existing request, and apply the upgrade on both SQLite and SQL Server in CI. Browser tests cover All services, family filters, search, mobile reflow and axe checks. This proves configured coverage of the reviewed index, not permanent completeness of a third-party website that can change.

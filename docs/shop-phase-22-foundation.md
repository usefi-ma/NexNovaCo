# Shop Phase 22 Foundation

## Ownership map

| Concern | Canonical owner | Notes |
| --- | --- | --- |
| Product catalog | `Products` | One ordered SQLite collection shared by the dashboard and public Shop page. |
| Product initialization | `ProductInitializationState` | Defaults are inserted only before the collection has ever been initialized. An intentionally empty catalog stays empty. |
| Product media | `Product.ImagePath` | One cover image per product in Phase 22. Uploaded files use `uploads/products/`; gallery media remains deferred to Phase 23. |
| Shop hero | `ShopHeroSettings` | Page-only eyebrow, title, description, CTA label, and hero image. The CTA target is fixed to the products section. |
| Shop products introduction | `ShopProductsSectionSettings` | Page-only eyebrow, title, and optional introduction. Product rows remain shared content. |
| Header and mobile navigation | Existing `NavigationItems` | Shop is part of fresh navigation defaults. Existing initialized navigation is never rewritten; an Admin can add Shop through Global Settings. |
| Site identity and footer | Existing global settings | Shop reuses the current public header, identity, footer, social links, contact information, and inert newsletter presentation. |

## Phase boundary decisions

- Phase 22 provides `/shop` and an accessible, visually disabled **View Product** button on each card. This avoids broken links while product-detail routing, gallery media, and related products remain deferred to Phase 23.
- Product cards use a dedicated dark angular presentation with cyan accents and an orange CTA. They do not reuse the generic Project card or a white dashboard-style card.
- The Shop hero uses an original, text-free image stored at `image/shop/shop-hero.png`; dashboard uploads for that field are isolated under `uploads/shop/`.
- Fresh databases receive six editable demo products. Public fallback content is read-only and is used only when the database cannot be read; intentional empty collections render an honest empty state.
- `/shop` is a static public discovery path. Product URLs and Product structured data are intentionally absent until Phase 23.

## Media policy

- Shop hero: JPG, PNG, or WebP, maximum 5 MB, controlled folder `uploads/shop/`.
- Product cover: JPG, PNG, or WebP, maximum 3 MB, controlled folder `uploads/products/`.
- Existing upload signature validation, path canonicalization, Admin authorization, and staged-upload workflow are reused without a new HTTP upload endpoint.

## Migration and initialization

The Phase 22 migration is additive. It creates the Product collection, its one-time marker, and the two Shop settings singletons. Startup initializes missing Shop singletons and seeds Products only when the marker is absent and the collection has never been populated. Admin edits are never overwritten.

## Verification record

- Focused Shop suite: 52 checks covering additive migration shape, prior-data preservation, singleton and collection initialization, CRUD/reorder, validation, Admin authorization, media isolation, read-only fallbacks, delete-all/restart, add-after-empty/restart, SEO, sitemap, and safe public empty rendering.
- Existing auth/CMS regression suite: 3,026 checks passed.
- Debug and Release solution builds: zero warnings and zero errors.
- Chrome 153: `/shop` and all three new Dashboard routes passed at 1440, 1024, 768, and 390 pixels.
- Edge 154 and Firefox 156: `/shop` and all three new Dashboard routes passed at 1440 and 390 pixels.
- All browser runs reported zero horizontal overflow, zero console/page errors, and zero failed local requests. Chrome additionally verified equal first-row card heights, hover, keyboard focus, reduced motion, mobile-menu behavior, CTA scrolling, staged Product image preview, the shared unsaved-changes guard, Product creation, and public rendering.

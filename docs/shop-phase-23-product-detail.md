# Shop Phase 23 — Product Detail, Gallery, and Related Products

## 1. Git

- Branch: `feature/dashboard-cms`
- Phase 23 is implemented as focused, local commits only.
- Nothing was pushed or merged.

## 2. Phase 22 upgrade safety

- `ProductEntity` remains the only canonical Product record.
- Existing Phase 22 fields, values, ordering, slugs, prices, badges, covers, and timestamps are preserved.
- The additive migration gives existing rows an empty detail column; one-time initialization fills only that new field.
- Approved demo Products receive approved detail content. An existing Admin-created Product receives its own short description as the safe initial full description.
- Deleted Products are never recreated.

## 3. Product Detail data model

`ProductEntity` now owns:

- `FullDescription`
- ordered `ProductGalleryImage` children (`Source`, `Alt`, `DisplayOrder`)
- ordered `ProductFeature` children (`Text`, `DisplayOrder`)
- ordered `ProductRelatedProduct` relations (`ProductId`, `RelatedProductId`, `DisplayOrder`)

The related relation has database constraints for unique pairs, unique order per Product, a maximum order of three, and no self-reference. Both foreign keys cascade so deleting either side removes stale relations safely.

## 4. Migration / initialization

- Migration: `20260928041234_AddProductDetails`
- Additive changes only: one column and four focused tables, plus indexes and constraints.
- `ProductDetailInitializationState` is a persistent singleton marker.
- Defaults run once after Product ids exist. Restart does not duplicate children, and intentionally emptied child collections remain empty.
- The normal developer database was not reset; migration and upgrade checks used isolated databases.

## 5. Shared Products CMS

The existing Product editor was extended rather than duplicated. It has six responsive tabs with one form and one shared Save action:

1. Card
2. Pricing
3. Detail
4. Gallery
5. Features
6. Related

The same editor owns existing card content, the new detail content, gallery, features, and related selection.

## 6. Product Gallery

- Supports multiple validated JPG/JPEG, PNG, and WebP uploads through the existing Product media service and `uploads/products/` folder.
- Supports preview, meaningful alt text, remove, Move Up, and Move Down.
- Public rendering rule: gallery images are the detail gallery; `Product.ImagePath` remains the card/SEO cover and is used as the detail fallback only when the gallery is empty.
- One image has no pointless controls. Multiple images have previous/next buttons, thumbnails, live position text, Arrow Left/Right support, touch/pen swipe, visible focus, and reduced-motion-safe CSS.
- Removing a gallery row does not physically delete the file. Orphan cleanup remains deferred.

## 7. Features

- Plain-text features support Add, Edit, Remove, Move Up, and Move Down.
- The public page uses a semantic list and hides the section when empty.

## 8. Related Products

- Admins explicitly choose, order, and remove up to three existing Products.
- Self-relations, duplicates, and missing Product ids are rejected in the service and constrained in the database where applicable.
- Public rendering follows the saved order and reuses `ProductCard`; there is no second card implementation and no carousel.
- Deleting a related Product cascades the relation, leaving the source detail page healthy.

## 9. Public Product Detail

- Route: `/shop/{slug}`
- Valid slugs render breadcrumb, gallery, Product information, safe multi-paragraph plain text, optional Features, and optional Related Products.
- Invalid and deleted slugs use framework not-found handling and never resurrect fallback records.
- The catalog-only action is `Back to Shop`; no Buy, Add to Cart, Checkout, availability, or other fake commerce behavior was added.
- Zero gallery, features, related items, original price, or badge are all safe states.

## 10. ProductCard CTA activation

- `View Product` is now a real link to `/shop/{slug}` on the Shop grid and Related Products.
- The component accepts a nested-heading option so related cards use `h3` without duplicating markup.

## 11. SEO / sitemap

- Product details use the shared `PublicPageHead` implementation for unique title, Product description, canonical URL, Open Graph, Twitter metadata, social image fallback, and breadcrumb structured data.
- The sitemap now reads current Product slugs directly from SQLite. Deleted Products disappear and invalid slugs are excluded.
- Product structured data is intentionally deferred to Phase 24; no fake availability, reviews, or purchasability were published.

## 12. Accessibility

- One public `h1`, semantic breadcrumbs, ordered headings, semantic Features list, and nested related-card headings.
- Gallery region, controls, thumbnail labels, image position announcement, meaningful alt text validation, keyboard navigation, and focus treatment are present.
- Current and original prices have explicit screen-reader labels; strikethrough and color are not the only meaning.

## 13. Responsive verification

Public Product Detail and the extended Dashboard editor were checked at 1440, 1024, 768, and 390 px.

- No horizontal overflow at any required width.
- Gallery and Product information remain side-by-side through the tablet breakpoint.
- At 390 px the gallery precedes Product information, related cards stack, controls remain reachable, and Dashboard tabs remain usable.
- Header and Footer remain intact.

## 14. Browser verification

- Real Chrome: 1440, 1024, 768, and 390 px passed, including gallery button/keyboard order, CTA route, repeated navigation, local resources, console, and Interactive Server behavior.
- Real Edge: 1440 and 390 px passed the same smoke checks.
- Codex in-app Chromium: visual inspection at every required width passed with no console warnings/errors.
- Installed Firefox was detected, but its remote automation endpoint did not become available to either the Playwright BiDi or Juggler backend in this environment. No Firefox result is claimed; this is the only outstanding environment-limited browser check.

## 15. Tests / builds

- 40 focused Product Detail/gallery/features/related checks pass.
- 53 focused Phase 22 Shop/Product CMS regression checks pass.
- 3,026 existing auth/CMS checks pass.
- Debug build: zero warnings and zero errors.
- Release build: zero warnings and zero errors.
- Covered: additive upgrade, custom Phase 22 Product preservation, one-time seed, restart, secure A/B/C/D uploads, D/A/B/C reorder, one/zero gallery, cover fallback, feature order, related order/removal/cascade, self/duplicate/missing relation rejection, metadata, sitemap removal, not-found, authorization, empty states, and restart persistence.

## 16. Deferred to Phase 24

- final Shop SEO review
- Product structured-data decision
- final visual polish
- final cross-browser Shop verification, including the remaining Firefox run
- performance sanity check

## 17. Deferred beyond Shop V1

- Cart, Add to Cart, Checkout, Payment, Orders, Inventory, Shipping, Taxes, Coupons, Reviews, Wishlist, and customer accounts
- search, filter, and category systems
- purchase email flows
- Media Library, cropper, orphan cleanup, and CDN/cloud storage

## 18. Recommendation

The implementation is ready for Phase 24 from a data, CMS, public-route, security, accessibility, persistence, Chrome, and Edge standpoint. Phase 24 should begin only after approval and should include the remaining Firefox verification on a host where Firefox automation is available.

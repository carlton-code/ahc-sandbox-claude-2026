# ADR-0013: Address writes split across two route prefixes

## Status

Accepted

## Context

Addresses shipped as a read-only sub-resource of `Customer`
(`GET /api/v1/customers/{customerId}/addresses[/{addressId}]`). Adding write support ran straight
into the shape of the schema: **`SalesLT.Address` has no owner column.** A customer is attached to
an address through `SalesLT.CustomerAddress`, which also carries `AddressType` (only ever
`Main Office` or `Shipping`).

That splits the resource in two, and the two halves want different routes:

- **Creating** an address without also writing a `CustomerAddress` row leaves an orphan — a row no
  read path in this API can reach, and a state no seeded data exhibits (all 450 addresses are
  linked). So a create needs a customer, and the `addressType` for the link.
- **Reading, editing and deleting** an address record needs no customer at all. `AddressType`
  isn't part of the address, so it isn't editable through those routes.

Three route shapes were considered:

1. **Everything nested under the customer.** No orphans possible, and it extends the existing read
   routes exactly — but `PUT /customers/{c}/addresses/{a}` implies the edit is scoped to that
   customer when it actually mutates a shared `Address` row, and it puts a customer id in the path
   of an operation that doesn't need one.
2. **Everything top-level at `/api/v1/addresses`.** The simplest controller, but `POST` then
   creates an address linked to nobody, and re-attaching it would need separate link/unlink
   endpoints — more surface, for a capability nothing has asked for.
3. **Hybrid** — create nested, everything else top-level.

Delete raised a second question, since every foreign key in this database is `NO_ACTION`
(see ADR-0009): should deleting an address remove its `CustomerAddress` link rows first so the
delete can succeed, or refuse?

## Decision

**Create is nested under the customer; read-by-id, update and delete are top-level on the address.
Delete refuses rather than unlinking.**

`AddressesController` therefore carries no class-level `[Route]` and each action declares an
absolute template — two `[Route]` attributes would not express this, because every action template
combines with every controller route, generating both prefixes for every action.

| Method | Path |
|---|---|
| `POST` | `/api/v1/customers/{customerId}/addresses` |
| `GET` | `/api/v1/addresses/{addressId}` |
| `PUT` | `/api/v1/addresses/{addressId}` |
| `DELETE` | `/api/v1/addresses/{addressId}` |

`POST`'s `Location` header points at the top-level `GET`, which is what ties the two prefixes
together. `AddressWriteRepository.CreateForCustomerAsync` writes the `Address` and its
`CustomerAddress` link in one `SaveChangesAsync`, referencing the address through the navigation
property so EF fixes the IDENTITY key into the link row's foreign key.

The two existing customer-scoped **reads** stay on `CustomersController` — they're list views of a
customer's addresses, they're the only place `addressType` is exposed, and they already ship.

Update takes no `addressType` (that's link data) and there is no `PATCH` — an address is a small
record replaced wholesale.

## Consequences

**`DELETE /api/v1/addresses/{addressId}` can only ever return `409`.** Creating an address through
this API always writes a link row, and all 450 seeded addresses have one, so nothing this endpoint
can reach is unreferenced. `204` is declared and implemented because the repository can return it,
not because a caller can provoke it. This is stricter than ADR-0009's customer delete, where `204`
is at least reachable for a freshly-created customer. Accepted deliberately: the alternative —
silently unlinking an address from its customer as a side effect of deleting it — hides a second
mutation inside a delete, and `SalesOrderHeader.ShipToAddressID`/`BillToAddressID` could still
block it anyway. If a real unlink use case appears, it should be its own explicit endpoint
(`DELETE /customers/{c}/addresses/{a}`), not a side effect of this one.

**One resource lives at two prefixes.** A reader scanning routes sees addresses in two places, and
the OpenAPI document lists them as separate paths. The controller's XML doc and `docs/api.md`
carry the explanation so the split reads as intentional.

**The delete-refuses guarantee needed an explicit EF mapping to be true.** `CustomerAddress`'s
required relationship to `Address` defaulted to `DeleteBehavior.Cascade`, which EF applies
*client-side* as well: deleting an address while its link row happened to be tracked made EF
quietly delete the link too, and the delete succeeded. The behavior therefore depended on what
else the `DbContext` had loaded. `.OnDelete(DeleteBehavior.ClientNoAction)` now matches the real
`NO_ACTION` foreign key, leaving tracked dependents alone so the database refuses and
`DatabaseConflictExceptionHandler` maps SQL 547 to `409`.

**No orphan address can be created through this API** — which also means there is no way to add an
address now and attach it to a customer later. That's the intended trade-off, not an oversight.

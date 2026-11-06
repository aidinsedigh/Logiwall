# ETipax shipping

Replace the active MyTipax connection with ETipax OM/v3, following the ShopLink wire contracts.
Keep the layered architecture and existing API response shapes. Preserve historical MyTipax records.

Cities remain internal reference data, initially sourced from Tipax. Provider city codes live in StaticDataMappers.
Packaging, package contents and status definitions belong to a provider. Orders use internal reference identifiers.
Store shipment and parcel snapshots, external identifiers, attempts and status history independently of payment state.
Accept authenticated, deduplicated Tipax webhooks. Synchronize reference catalogues using a disabled-by-default background job.

Never automatically repeat an ambiguous registration. Never invent a tracking or city endpoint absent from the source contract.
Registration is an explicit owner-authorized action after service selection. Online-paid shipments require verified payment.
No real shipping, payment, production migration or deployment is part of verification.

Provider snapshots contain personal data: restrict access and never expose raw payloads through customer APIs or logs.

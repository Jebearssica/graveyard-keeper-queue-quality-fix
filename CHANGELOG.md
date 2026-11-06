# Changelog

## 1.0.0 | 6 November 2026

- First release.
- Queued quality crafts (Queue Everything's *Allow Multi-Quality Multicraft*) keep the star tier picked in the craft
  window instead of starting with `multiquality_ids = null`. Previously the game threw a `NullReferenceException`
  in `CraftDefinition.GetMultiqualityResult` after the ingredients and fuel had already been removed, so the craft
  produced nothing and the queue entry was lost.
- Falls back to vanilla's "no particular star" tier-draining behaviour when no selection was remembered (saved queue,
  cloned queue rows), which cannot throw.

# Forgejo Label Sync

[!WARNING]
> Labels added to `labels.json` must **always** be added to the end of the array and should **never** be removed.
> Array indexes are mapped to Forgejo repository and label IDs. This makes it possible to update labels.
> For this program to function correctly, the order of labels added in labels.json must never change.
> To remove a label from your repositories, add `deprecated: true`.
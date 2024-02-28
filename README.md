# Forgejo Label Sync

A docker image that allows you to sync label changes across your Forgejo instance.

## Introduction

I created Forgejo Label Sync because I am very opinionated about my labels, and sometimes I want to add new labels or change something about my current labels. Adding a new label to hundreds of repositories is an extremely time consuming task, and updating thousands of labels across all of those repositories to simply change a label name or color is even worse.

That's where Forgejo Label Sync comes in to save the day! Here is how it works!

1. You setup `settings.json` with your forgejo instance url, api key, and include / exclude filters if you want them.

2. You create a `labels.json` that defines all of your labels as you want them.

3. When you first run Forgejo Label Sync, it will do the following:

- Create a link between your repositories and pre-existing labels with your labels defined in `labels.json`. This link is based on matching label names.

- Create missing labels for your repositories, as defined in `labels.json`.

- Update your repository labels with any changes to `labels.json`.

- Create a link between the newly created repository labels and `labels.json`.

Forgejo Label Sync will sync the following changes to labels:

- Name
- Description
- Color
- Exclusive
- Archive

## Limitations

Forgejo Label Sync creates links between your repository labels in your Forgejo instance by associating your repository ID and label ID with the index of the label in `labels.json`.

This means that changing the order of labels and deleting labels in `labels.json` will result in the the index changing, making the established link between labels broken.

In a future update, this will be addressed by allowing you to rebuild the links in the database from scratch by passing a flag or environmental variable.

## Installation

### Docker Compose

```docker
name: ofelia

services:
  ofelia:
    image: mcuadros/ofelia:latest
    container_name: ofelia
    depends_on:
      - forgejo-label-sync
    labels:
      ofelia.job-run.forgejo-label-sync.schedule: "@every 15m"
      ofelia.job-run.forgejo-label-sync.container: "forgejo-label-sync"
      ofelia.job-run.forgejo-label-sync.no-overlap: true
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
    command: daemon --docker
    restart: unless-stopped

  forgejo-label-sync:
    image: code.douglasparker.dev/forgejo/label-sync:latest
    container_name: forgejo-label-sync
    volumes:
      - forgejo-label-sync:/app/data

volumes:
  forgejo-label-sync:
```

#### Edit Settings

`docker run --rm -it -v ofelia_forgejo-label-sync:/app/data code.douglasparker.dev/os/alpine:latest nano /app/data/settings.json`

#### Edit Labels

`docker run --rm -it -v ofelia_forgejo-label-sync:/app/data code.douglasparker.dev/os/alpine:latest nano /app/data/labels.json`
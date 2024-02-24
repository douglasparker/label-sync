-- CreateTable
CREATE TABLE "Label" (
    "id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "label_id" INTEGER NOT NULL,
    "repository_id" INTEGER NOT NULL,
    "custom_label_id" INTEGER NOT NULL
);

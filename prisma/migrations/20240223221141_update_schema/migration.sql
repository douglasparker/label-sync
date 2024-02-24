/*
  Warnings:

  - You are about to drop the column `custom_label_id` on the `Label` table. All the data in the column will be lost.
  - You are about to drop the column `repository_name` on the `Label` table. All the data in the column will be lost.
  - Added the required column `label_index` to the `Label` table without a default value. This is not possible if the table is not empty.
  - Added the required column `repository` to the `Label` table without a default value. This is not possible if the table is not empty.

*/
-- RedefineTables
PRAGMA foreign_keys=OFF;
CREATE TABLE "new_Label" (
    "id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "label_id" INTEGER NOT NULL,
    "label_index" INTEGER NOT NULL,
    "repository" TEXT NOT NULL
);
INSERT INTO "new_Label" ("id", "label_id") SELECT "id", "label_id" FROM "Label";
DROP TABLE "Label";
ALTER TABLE "new_Label" RENAME TO "Label";
PRAGMA foreign_key_check;
PRAGMA foreign_keys=ON;

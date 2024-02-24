/*
  Warnings:

  - You are about to drop the column `repository_id` on the `Label` table. All the data in the column will be lost.
  - Added the required column `repository_name` to the `Label` table without a default value. This is not possible if the table is not empty.

*/
-- RedefineTables
PRAGMA foreign_keys=OFF;
CREATE TABLE "new_Label" (
    "id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "label_id" INTEGER NOT NULL,
    "repository_name" TEXT NOT NULL,
    "custom_label_id" INTEGER NOT NULL
);
INSERT INTO "new_Label" ("custom_label_id", "id", "label_id") SELECT "custom_label_id", "id", "label_id" FROM "Label";
DROP TABLE "Label";
ALTER TABLE "new_Label" RENAME TO "Label";
PRAGMA foreign_key_check;
PRAGMA foreign_keys=ON;

-- Correctif de modélisation sur pal.best_work_suitability_id (V2.43.0).
--
-- Le champ brut source "Others.BestWorkSuitability" (ex: MonsterFarm, Handcraft,
-- EmitFlame...) utilise un vocabulaire DIFFÉRENT de celui de work_suitability.json
-- (ex: Farming, Handiwork, Kindling...). Ce n'est pas la même notion : certains
-- pals (29/299, ex: "MonsterFarm") n'ont aucun équivalent parmi les 12 aptitudes
-- de travail référencées. Une FK vers work_suitability aurait nécessité une table
-- de correspondance devinée/non fiable. On stocke donc la valeur brute telle
-- quelle, sans relation.
ALTER TABLE tools_palworld.pal DROP COLUMN best_work_suitability_id;
ALTER TABLE tools_palworld.pal ADD COLUMN best_work_suitability_label VARCHAR(50);

COMMENT ON COLUMN tools_palworld.pal.best_work_suitability_label IS 'Valeur brute de Others.BestWorkSuitability (vocabulaire différent des aptitudes de travail référencées, cf. commentaire de migration)';

-- Complément de modélisation sur pal (V2.43.0).
--
-- La tierlist affichait "vitesse min - max" via un scrape séparé de
-- palworld.gg, alors que paldb.cc expose déjà ces valeurs dans
-- raw.Movement (RunSpeed / RideSprintSpeed), jamais captées jusqu'ici par
-- le scraper de pals.json (seuls raw.Stats et raw.Others étaient lus).
-- RunSpeed = vitesse de course du Pal (sauvage). RideSprintSpeed = vitesse
-- en sprint une fois chevauché, absente pour les Pals non montables.
ALTER TABLE tools_palworld.pal ADD COLUMN run_speed INTEGER;
ALTER TABLE tools_palworld.pal ADD COLUMN ride_sprint_speed INTEGER;

COMMENT ON COLUMN tools_palworld.pal.run_speed IS 'Vitesse de course du Pal (raw.Movement.RunSpeed chez paldb.cc)';
COMMENT ON COLUMN tools_palworld.pal.ride_sprint_speed IS 'Vitesse en sprint une fois chevauché (raw.Movement.RideSprintSpeed), NULL si non chevauchable';

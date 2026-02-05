ALTER TABLE tools_dofus.workshop
ADD COLUMN is_pinned BOOLEAN NOT NULL DEFAULT FALSE;

COMMENT ON COLUMN tools_dofus.workshop.is_pinned IS 'Indique si l''atelier est épinglé dans la navigation';
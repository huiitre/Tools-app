id | workshop_item_id | ingredient_id           | parent_ingredient_id | quantity_required | quantity_obtained (virtuel) | is_crafted (virtuel)
---|------------------|-------------------------|----------------------|-------------------|-------------------|------------
-- Niveau 1 (racine - ingrédients du Tacleur majeur)
10 | 1                | kobalite                | NULL                 | 4                 | 0                 | true
11 | 1                | tacleur                 | NULL                 | 1                 | 0                 | true
12 | 1                | substrat_foret          | NULL                 | 1                 | 0                 | true
13 | 1                | oeil_branche            | NULL                 | 2                 | 0                 | false
14 | 1                | galet_rayonnant         | NULL                 | 2                 | 0                 | false
15 | 1                | corde_fancrôme          | NULL                 | 5                 | 0                 | false
16 | 1                | pierre_serpiplume       | NULL                 | 5                 | 0                 | false
17 | 1                | masque_araknotron       | NULL                 | 5                 | 0                 | false

-- Niveau 2 (ingrédients de la Kobalite)
20 | 1                | etain                   | 10                   | 5                 | 0                 | false
21 | 1                | silicate                | 10                   | 5                 | 0                 | false
22 | 1                | argent                  | 10                   | 10                | 0                 | false
23 | 1                | kobalte                 | 10                   | 10                | 0                 | false
24 | 1                | manganèse               | 10                   | 10                | 0                 | false
25 | 1                | bauxite                 | 10                   | 10                | 0                 | false

-- Niveau 2 (ingrédients du Substrat de Forêt)
30 | 1                | potion_ancetres         | 12                   | 1                 | 0                 | true
31 | 1                | planche_gravure         | 12                   | 1                 | 0                 | false
32 | 1                | potion_vieillesse       | 12                   | 1                 | 0                 | false

-- Niveau 2 (ingrédients du Tacleur)
40 | 1                | kouartz                 | 11                   | 1                 | 0                 | false
41 | 1                | tacleur_mineur          | 11                   | 1                 | 0                 | true
42 | 1                | substrat_bosquet        | 11                   | 1                 | 0                 | false
43 | 1                | galet_rutilant          | 11                   | 2                 | 0                 | false
44 | 1                | corbac_mort             | 11                   | 5                 | 0                 | false
45 | 1                | scalp_bizarbwork        | 11                   | 5                 | 0                 | false
46 | 1                | oreille_rhinoféroce     | 11                   | 10                | 0                 | false

-- Niveau 3 (ingrédients de la Potion des Ancêtres)
50 | 1                | malt                    | 30                   | 10                | 0                 | false
51 | 1                | seigle                  | 30                   | 20                | 0                 | false

-- Niveau 3 (ingrédients du Tacleur mineur)
60 | 1                | magnésite               | 41                   | 3                 | 0                 | true
61 | 1                | substrat_futaie         | 41                   | 1                 | 0                 | false
62 | 1                | galet_cramoisi          | 41                   | 2                 | 0                 | false
63 | 1                | peau_larve_saphir       | 41                   | 5                 | 0                 | false
64 | 1                | fragment_pierre_polie   | 41                   | 10                | 0                 | false
65 | 1                | pic_dragodinne          | 41                   | 10                | 0                 | false

-- Niveau 4 (ingrédients du Magnésite)
70 | 1                | fer                     | 60                   | 10                | 0                 | false
71 | 1                | cuivre                  | 60                   | 10                | 0                 | false
72 | 1                | bronze                  | 60                   | 10                | 0                 | false
73 | 1                | kobalte                 | 60                   | 10                | 0                 | false
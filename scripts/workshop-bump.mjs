#!/usr/bin/env node
import { bumpWorkshop } from '@rimworks/mod-ci';

const stagePath = await bumpWorkshop({
  workshopId: process.env.WORKSHOP_ID || '3791648678',
  solution: 'Pickle.slnx',
  // without it the stamp has no RimWorld line. CI exports it from stage-game-refs
  refsDir: process.env.GameManagedDir,
});

console.log(`pushed from ${stagePath}`);

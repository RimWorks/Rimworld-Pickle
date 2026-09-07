#!/usr/bin/env node
import { bumpWorkshop } from '@rimworks/mod-ci';

const stagePath = await bumpWorkshop({
  workshopId: process.env.WORKSHOP_ID || '3791648678',
  solution: 'Pickle.slnx',
});

console.log(`pushed from ${stagePath}`);

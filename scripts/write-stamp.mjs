#!/usr/bin/env node
import { writeStamp } from '@rimworks/mod-ci';

// the shared package holds the logic, this file holds the one thing that is repo specific
process.stdout.write(await writeStamp({ solution: 'Pickle.slnx' }));

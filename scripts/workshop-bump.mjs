import { stageModContent } from 'semantic-release-steam/lib/stage-content.mjs';
import { uploadWorkshopItem } from 'semantic-release-steam/lib/steamcmd.mjs';
import { writeStamp } from './write-stamp.mjs';

const APP_ID = '294100';
const WORKSHOP_ID = process.env.PICKLE_WORKSHOP_ID ?? '3791648678';

function required(name) {
  const value = process.env[name];
  if (!value) throw new Error(`${name} is required`);
  return value;
}

const steamCmdPath = required('STEAMCMD_PATH');
const steamUsername = required('STEAM_USERNAME');
const steamConfigPath = required('STEAM_CONFIG_VDF');

if (!WORKSHOP_ID) throw new Error('no workshop id configured');

// the build is deterministic, so unchanged source rebuilds byte for byte. Steam moves the
// Updated date only when the content manifest changes, so the stamp is what makes it move.
await writeStamp();
const stagePath = await stageModContent({ modPath: process.cwd() });

await uploadWorkshopItem({
  steamCmdPath,
  steamUsername,
  steamConfigPath,
  stagePath,
  appId: APP_ID,
  publishedFileId: WORKSHOP_ID,
  changenote: `Weekly verification run against RimWorld ${process.env.VERIFIED_RIMWORLD ?? 'current'}. No code changes.`,
  verbose: true,
  logger: console,
});

console.log(`pushed ${WORKSHOP_ID} from ${stagePath}`);

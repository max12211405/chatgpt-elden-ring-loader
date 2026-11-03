const {spawnSync}=require('node:child_process');const path=require('node:path');
const tests=['test_keyboard_bridge.cjs','test_recovery.cjs','test_soft_entry.cjs','test_vortex.cjs','test_warm_entry.cjs','test_music.cjs'];
for(const test of tests){console.log('\n'+test);const result=spawnSync(process.execPath,[path.join(__dirname,test)],{stdio:'inherit',env:process.env});if(result.status!==0)process.exit(result.status||1);}

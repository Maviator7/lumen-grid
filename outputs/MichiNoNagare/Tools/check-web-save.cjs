const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const file=require('node:path').join(__dirname,'../Assets/Michi/Plugins/WebGL/CitySave.jslib');
const disk=new Map();let blocked=false,dropped=false,lib;
const storage={getItem:k=>{if(blocked)throw Error('blocked');return disk.get(k)??null;},setItem:(k,v)=>{if(blocked)throw Error('quota');if(!dropped)disk.set(k,v);}};
function boot(){const scope={localStorage:storage,UTF8ToString:x=>x,stringToNewUTF8:x=>x,LibraryManager:{library:{}},mergeInto:(a,b)=>Object.assign(a,b),console:{error:()=>{}},JSON};vm.runInNewContext(fs.readFileSync(file,'utf8'),scope);lib=scope.LibraryManager.library;}
boot();assert.equal(lib.LumenSave_Read('city'),'');assert.equal(lib.LumenSave_Write('city','A',''),1);boot();assert.equal(JSON.parse(lib.LumenSave_Read('city')).Primary,'A');
assert.equal(lib.LumenSave_Write('city','B','A'),1);boot();assert.deepEqual(JSON.parse(lib.LumenSave_Read('city')),{Primary:'B',Backup:'A'});
blocked=true;assert.equal(lib.LumenSave_Write('city','C','B'),0);blocked=false;assert.equal(JSON.parse(lib.LumenSave_Read('city')).Primary,'B');
dropped=true;assert.equal(lib.LumenSave_Write('city','C','B'),0);dropped=false;assert.equal(JSON.parse(lib.LumenSave_Read('city')).Backup,'A');
console.log('PASS: Web save reload, atomic backup, blocked storage and read-back failure');

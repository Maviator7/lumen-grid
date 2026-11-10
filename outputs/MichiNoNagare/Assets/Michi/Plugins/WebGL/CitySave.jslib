mergeInto(LibraryManager.library, {
  LumenSave_Read: function(key) {
    try { return stringToNewUTF8(localStorage.getItem(UTF8ToString(key)+'.durable') || ''); }
    catch (e) { console.error('LUMEN save read failed',e); return stringToNewUTF8(''); }
  },
  LumenSave_Write: function(key,primary,backup) {
    try {
      var k=UTF8ToString(key)+'.durable';
      var value=JSON.stringify({Primary:UTF8ToString(primary),Backup:UTF8ToString(backup)});
      localStorage.setItem(k,value);
      if(localStorage.getItem(k)!==value)return 0;
      if(typeof window!=='undefined' && window.lumenServerBackupEnabled){
        fetch('/api/city-save',{method:'POST',headers:{'Content-Type':'application/json'},body:value,keepalive:false})
          .then(function(r){if(!r.ok)throw Error('PC backup failed');SendMessage('Lumen Network','ReceivePCBackupStatus','OK');})
          .catch(function(e){console.error('LUMEN PC backup failed',e);SendMessage('Lumen Network','ReceivePCBackupStatus','PCバックアップ未完了。ブラウザ内の保存は完了しています。');});
      }
      return 1;
    } catch(e) { console.error('LUMEN save write failed',e);return 0; }
  },
  LumenSave_Download: function(json,name) {
    var url=URL.createObjectURL(new Blob([UTF8ToString(json)],{type:'application/json'}));
    var a=document.createElement('a');a.href=url;a.download=UTF8ToString(name);
    document.body.appendChild(a);a.click();a.remove();setTimeout(function(){URL.revokeObjectURL(url);},30000);
  },
  LumenSave_Upload: function(target) {
    var objectName=UTF8ToString(target),input=document.createElement('input');
    input.type='file';input.accept='.json,application/json';
    input.addEventListener('change',function(){
      var file=input.files[0];if(!file)return;
      if(file.size>900000){SendMessage(objectName,'ReceiveSaveError','セーブファイルが大きすぎます。');return;}
      file.text().then(function(text){SendMessage(objectName,'ReceiveSaveFile',text);})
        .catch(function(){SendMessage(objectName,'ReceiveSaveError','ファイルを読み込めませんでした。');});
    });input.click();
  }
});

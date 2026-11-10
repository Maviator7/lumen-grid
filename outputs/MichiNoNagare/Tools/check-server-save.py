import base64,hashlib,importlib.util,json,tempfile,unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location('serve',Path(__file__).with_name('serve-web.py'));serve=importlib.util.module_from_spec(spec);spec.loader.exec_module(serve)
def envelope(seed):
 data=json.dumps({'Version':1,'SavedAt':'2026-11-11T00:00:00Z','Network':{'Seed':seed}})
 return json.dumps({'Version':1,'Data':data,'Checksum':base64.b64encode(hashlib.sha256(data.encode()).digest()).decode()})
class SaveBackupTests(unittest.TestCase):
 def test_restart_backup_and_reject_invalid(self):
  with tempfile.TemporaryDirectory() as tmp:
   store=serve.CityBackupStore(Path(tmp));self.assertIsNone(store.load())
   a,b=envelope(42),envelope(43);store.save({'Primary':a,'Backup':''});store.save({'Primary':b,'Backup':a})
   self.assertEqual(serve.CityBackupStore(Path(tmp)).load()['Primary'],b)
   with self.assertRaises(ValueError):store.save({'Primary':'broken','Backup':a})
   self.assertEqual(store.load()['Primary'],b)
   newest=sorted(Path(tmp).glob('*.json'))[-1];newest.write_text('broken')
   self.assertEqual(store.load()['Primary'],a)
 def test_checksum(self):
  with tempfile.TemporaryDirectory() as tmp:
   bad=json.loads(envelope(1));bad['Data']+='x'
   with self.assertRaises(ValueError):serve.CityBackupStore(Path(tmp)).save({'Primary':json.dumps(bad),'Backup':''})
if __name__=='__main__':unittest.main()

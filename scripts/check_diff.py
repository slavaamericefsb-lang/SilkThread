import re, glob
cat_ids = set()
for f in glob.glob('src/SilkThread.Core/TweakDatabase/*Tweaks.cs'):
    with open(f, encoding='utf-8') as fh:
        cat_ids.update(re.findall(r'id:\s*"([^"]+)"', fh.read()))

import sys
sys.path.insert(0, 'scripts')
import generate_translations
loc_ids = set(generate_translations.tweaks.keys())

diff1 = cat_ids - loc_ids
diff2 = loc_ids - cat_ids
print('In catalog but not in loc:', diff1)
print('In loc but not in catalog:', diff2)

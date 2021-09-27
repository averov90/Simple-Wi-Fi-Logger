import random

def allowed(item, strlist):
    return all([characters in list(strlist) for characters in item])

def item_good(item):
    if len(item) == 0:
        return False;
    return allowed(item[1:].upper(), "0123456789ABCDEF") and item[0] == '$'

lines = []
first = ""

with open('log_0_.csv', 'r', encoding="utf-8") as file:
    first = file.readline()
    lines = file.readlines()

st = {}
for line in lines:
    st[line.split(';')[1].strip()] = line.split(';')[7].strip()


for item in st:
    if item_good(st[item][-7:]):
        lines = [w.replace(item, item[:-8] + st[item][-6:-4] + ':' + st[item][-4:-2] + ':' + st[item][-2:]) for w in lines]
    elif item_good(st[item][-5:]):
        lines = [w.replace(item, item[:-8] + ("%02x:" % (random.randint(0, 255))).upper() + st[item][-4:-2] + ':' + st[item][-2:]) for w in lines]
    elif item_good(st[item][-3:]):
        lines = [w.replace(item, item[:-8] + ("%02x:%02x:" % (random.randint(0, 255), random.randint(0, 255))).upper() + st[item][-2:]) for w in lines]
    else:
        lines = [w.replace(item, item[:-8] + ("%02x:%02x:%02x" % (random.randint(0, 255), random.randint(0, 255), random.randint(0, 255))).upper()) for w in lines]
    
    #lines = [w.replace(item, newn) for w in lines]
lines = [w.replace('$', '') for w in lines]

with open('log_1_.csv', 'w', encoding="utf-8") as file:
    file.write(first)
    for item in lines:
        file.write("%s" % item)

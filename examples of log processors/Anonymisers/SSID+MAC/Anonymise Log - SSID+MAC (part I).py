import random

lines = []
first = ""

def allowed(item, strlist):
    return all([characters in list(strlist) for characters in item])

def item_good(item):
    return allowed(item[1:].upper(), "0123456789ABCDEF") and (item[0] == '-' or item[0] == '_')

with open('log_0_.csv', 'r', encoding="utf-8") as file:
    first = file.readline()
    lines = file.readlines()

def isAutoValid(item):
    if len(item) == 0:
        return False
    if item_good(item[-7:]) or item_good(item[-5:]) or item_good(item[-3:]):
        return True
    else:
        return False

def getUniName(fname):
    if ('$' in fname):
        return newn[:newn.find('$')]
    else:
        return fname

st = set()
for line in lines:
    st.add(line.split(';')[7].strip())

prewlines = []

used_names = []
used_names_prew = []

current = 0
setlist = list(st)
while current < len(setlist):
    item = setlist[current]
    current += 1
    print('|' + item + '| $valid:' + str(isAutoValid(item)) + ' ' + str(current) + '/' + str(len(setlist)))
    if item != '':
        newn = input('|')
        if newn == '_':
            print(item)
            print()
            continue
        if len(newn) == 0:
            if item_good(item[-7:]):
                newn = item[:-6] + '$' + ("%02x%02x%02x" % (random.randint(0, 255), random.randint(0, 255), random.randint(0, 255))).upper()
            elif item_good(item[-5:]):
                newn = item[:-4] + '$' + ("%02x%02x" % (random.randint(0, 255), random.randint(0, 255))).upper()
            elif item_good(item[-3:]):
                newn = item[:-2] + '$' + ("%02x" % (random.randint(0, 255))).upper()
            else:
               newn = item
        elif allowed(newn, '-'):
            if (len(prewlines) == 0):
                current -= 1
                print('no backward')
                continue
            lines = prewlines.copy()
            used_names = used_names_prew.copy()
            prewlines = []
            used_names_prew = []
            current -= 2
            continue
        elif newn[0] == '$':
            if (newn.count('$') == 1):
                newn = item + '_$' + ("%02x%02x" % (random.randint(0, 255), random.randint(0, 255))).upper()
            elif (newn.count('$') == 2):
                newn = item + '_$' + ("%02x%02x%02x" % (random.randint(0, 255), random.randint(0, 255), random.randint(0, 255))).upper()
            else:
                newn = item + '_$' + ("%02x" % (random.randint(0, 255))).upper()
        elif allowed(newn, " $") and newn.count('$') > 0 and newn.count(' ') > 0:
            pos = newn.find('$')
            if (newn.count('$') == 1):
                newn = item[:pos] + '$' + ("%02x%02x" % (random.randint(0, 255), random.randint(0, 255))).upper()
            elif (newn.count('$') == 2):
                newn = item[:pos] + '$' + ("%02x%02x%02x" % (random.randint(0, 255), random.randint(0, 255), random.randint(0, 255))).upper()
            else:
                newn = item[:pos] + '$' + ("%02x" % (random.randint(0, 255))).upper()
        elif newn[0] == '-' and newn[-1] == '$':
            if (newn.count('$') == 1):
                newn = item + newn[1:] + ("%02x%02x" % (random.randint(0, 255), random.randint(0, 255))).upper()
            elif (newn.count('$') == 2):
                newn = item + newn[1:-1] + ("%02x%02x%02x" % (random.randint(0, 255), random.randint(0, 255), random.randint(0, 255))).upper()
            else:
                newn = item + newn[1:-(newn.count('$')-1)] + ("%02x" % (random.randint(0, 255))).upper()
        elif newn[-1] == '$':
            if (newn.count('$') == 1):
                newn = newn + ("%02x%02x" % (random.randint(0, 255), random.randint(0, 255))).upper()
            elif (newn.count('$') == 2):
                newn = newn[:-1] + ("%02x%02x%02x" % (random.randint(0, 255), random.randint(0, 255), random.randint(0, 255))).upper()
            else:
                newn = newn[:-(newn.count('$')-1)] + ("%02x" % (random.randint(0, 255))).upper()

        if getUniName(newn) in used_names:
            print('|' + newn + '|$dup')
        else:
            print(newn)
        print()
        used_names_prew = used_names.copy()
        prewlines = lines.copy()

        used_names.append(getUniName(newn))
        
        lines = [w.replace(item, newn) for w in lines]

with open('log_1_.csv', 'w', encoding="utf-8") as file:
    file.write(first)
    for item in lines:
        file.write("%s" % item)

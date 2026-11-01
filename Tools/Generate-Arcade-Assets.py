from pathlib import Path
root = Path(__file__).resolve().parent.parent
# The game ships only its finished static fonts. This dependency-free helper
# regenerates the web logo and favicon without retaining source fonts or tooling.
glyphs = {
'D':['11110','11011','11001','11001','11001','11011','11110'],
'R':['11110','11011','11011','11110','11100','11010','11011'],
'O':['01110','11011','11011','11011','11011','11011','01110'],
'P':['11110','11011','11011','11110','11000','11000','11000'],
'S':['01111','11000','11000','01110','00011','00011','11110'],
'T':['11111','01110','00100','00100','00100','00100','00100'],
'A':['01110','11011','11011','11111','11011','11011','11011'],
'C':['01111','11000','11000','11000','11000','11000','01111'],
'K':['11011','11010','11100','11100','11110','11011','11011']}
parts=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 170" role="img" aria-label="Drop Stack">']
for word,oy,ox,color in [('DROP',0,35,'#eff8ff'),('STACK',90,5,'#15dcff')]:
    for k,letter in enumerate(word):
        for y,row in enumerate(glyphs[letter]):
            for x,value in enumerate(row):
                if value=='1':
                    px=ox+(k*6+x)*10; py=oy+y*10
                    parts.extend([f'<path fill="#031320" d="M{px} {py+3}h9v9h-9z"/>',f'<path fill="{color}" d="M{px} {py}h9v9h-9z"/>',f'<path fill="#fff" opacity=".42" d="M{px+1} {py+1}h7v1h-7z"/>'])
parts.append('</svg>')
(root/'Assets/WebGLTemplates/DropStack/logo.svg').write_text(''.join(parts),encoding='utf8')
(root/'Assets/WebGLTemplates/DropStack/favicon.svg').write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64"><path fill="#071423" d="M0 0h64v64H0z"/><path fill="#15dcff" stroke="#8eecff" stroke-width="2" d="M8 8h20v20H8zm0 24h20v20H8zm24 0h20v20H32z"/></svg>',encoding='utf8')

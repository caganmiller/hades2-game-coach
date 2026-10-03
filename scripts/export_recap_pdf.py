"""Two-page, local-only run summary. Full item details live in the HTML export."""
import json, os, sys
from pathlib import Path
from xml.sax.saxutils import escape
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Image, PageBreak, KeepTogether
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

def make(source, output):
    payload=json.loads(Path(source).read_text(encoding='utf-8-sig'))
    r=payload['run']; groups=payload['groups']
    fonts=Path(os.environ.get('WINDIR','C:/Windows'))/'Fonts'
    for name,file in [('Body','segoeui.ttf'),('Bold','seguisb.ttf'),('Title','georgia.ttf')]:
        pdfmetrics.registerFont(TTFont(name,str(fonts/file)))
    ink=colors.HexColor('#193a38'); green=colors.HexColor('#216b59'); gold=colors.HexColor('#766232')
    styles={
        'body':ParagraphStyle('body',fontName='Body',fontSize=10,leading=14,textColor=ink,spaceAfter=8),
        'small':ParagraphStyle('small',fontName='Body',fontSize=8,leading=11,textColor=colors.HexColor('#536b66'),spaceAfter=7),
        'title':ParagraphStyle('title',fontName='Title',fontSize=33,leading=40,textColor=ink,spaceAfter=12),
        'h2':ParagraphStyle('h2',fontName='Bold',fontSize=20,leading=26,textColor=green,spaceBefore=10,spaceAfter=12,keepWithNext=True),
        'h3':ParagraphStyle('h3',fontName='Bold',fontSize=13,leading=18,textColor=gold,spaceBefore=7,spaceAfter=7,keepWithNext=True),
    }
    def p(s,style='body'): return Paragraph(escape(str(s or '')).replace('\n','<br/>'),styles[style])
    def num(n): return 'Not recorded' if n is None else f'{n:,.0f}'
    story=[p(f'GAME COACH / HADES II / NIGHT {r["number"]} / SUMMARY','small'),p('Victory earned.' if r['cleared'] else 'A night to build on.','title'),p(f'{r["result"]} - {r["route"]}'),p(f'{r["weapon"]} / {r["aspect"]}'),Spacer(1,9)]
    stats=Table([[p('CLEAR TIME' if r['cleared'] else 'GAMEPLAY TIME','small'),p('FEAR','small'),p('DAMAGE TAKEN','small')],[p(payload['time'],'h3'),p(num(r.get('fear')),'h3'),p(num(r.get('damageTaken')),'h3')]],colWidths=[174]*3)
    stats.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,-1),colors.HexColor('#e7f0eb')),('BOX',(0,0),(-1,-1),.6,colors.HexColor('#83a597')),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),12)]))
    story += [stats,p('The build, connected','h2'),p('The three leading recorded synergy groups. Read the Full build HTML for every component and effect.','small')]
    for index,group in enumerate(groups[:3],1):
        story.append(KeepTogether([p(f'{index:02d} / {group["title"]}','h3'),p(group['why']),p(' + '.join(x['name'] for x in group['items']),'small'),p('To make it work: '+group['condition'],'small'),Spacer(1,6)]))
    if not groups: story.append(p('No supported synergy group is confirmed by the retained data. The full HTML contains the recorded loadout.'))
    story += [Spacer(1,8),p(f'{len(r["items"])} recorded effects / {len(r.get("choices") or [])} retained choices / {num(r.get("rooms"))} room visits','small'),p('Interactions describe potential synergy, not measured uptime or attributed damage. Missing historical levels and effects remain unknown.','small'),PageBreak(),p('The run at a glance','h2'),p(r.get('resultEvidence') or 'Game-save history','small')]
    def leaders(key,title):
        rows=[[p(title,'h3')]]
        for x in (r.get(key) or [])[:5]: rows.append([p(f'{x["name"]}  /  {num(x["amount"])}')])
        if len(rows)==1: rows.append([p('Not retained for this night.','small')])
        table=Table(rows,colWidths=[247]);table.setStyle(TableStyle([('VALIGN',(0,0),(-1,-1),'TOP'),('LINEBELOW',(0,1),(-1,-1),.3,colors.HexColor('#c5d6cf')),('LEFTPADDING',(0,0),(-1,-1),0)]));return table
    side=Table([[leaders('inflicted','Damage leaders'),leaders('suffered','Damage taken from')]],colWidths=[261]*2)
    side.setStyle(TableStyle([('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),0)]))
    story += [side,Spacer(1,12),p(f'Save-recorded totals: {num(r.get("damageDealt"))} dealt / {num(r.get("damageTaken"))} taken. Visible top-five leaders are separate from these totals.','small')]
    image=payload.get('image')
    if image and Path(image).is_file():
        story += [p('Your victory screen','h3')];shot=Image(image);ratio=min(522/shot.drawWidth,275/shot.drawHeight);shot.drawWidth*=ratio;shot.drawHeight*=ratio;story.append(shot)
    else:
        story += [p('Run context','h3'),p('Keepsakes: '+(' > '.join(r.get('keepsakes') or []) or 'Not retained')),p('The HTML export includes the complete available loadout and choice history.')]
    story += [Spacer(1,12),p('Explore the full build','h3'),p('Open the companion HTML file in any browser. Hover or tap boon icons for saved levels, rarity and descriptions; explore all synergy groups and the choice timeline.','small'),p('Created locally with Game Coach by Caganmiller. Hades II artwork belongs to Supergiant Games. No credentials, account settings or voice transcripts are included.','small')]
    def page(c,doc):
        c.setStrokeColor(green);c.setLineWidth(.7);c.line(45,755,567,755);c.setFillColor(ink);c.setFont('Body',8);c.drawString(45,24,f'GAME COACH BY CAGANMILLER / NIGHT {r["number"]} / SUMMARY');c.drawRightString(567,24,str(doc.page))
    SimpleDocTemplate(output,pagesize=(612,792),leftMargin=45,rightMargin=45,topMargin=48,bottomMargin=45,title=f'Night {r["number"]} - Run summary',author='Game Coach by Caganmiller').build(story,onFirstPage=page,onLaterPages=page)

if __name__=='__main__': make(sys.argv[1],sys.argv[2])
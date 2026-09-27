// Run against DetailTemplateBrowserHost's isolated DB, never a normal developer host.
// node scripts/Test-ProjectMediaBrowser.cjs <fixture.json> <playwright module> [verify|states|setup|empty]
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium,firefox}=require(process.argv[3]);
const fixture=JSON.parse(fs.readFileSync(process.argv[2],'utf8'));
const out=path.dirname(path.resolve(process.argv[2])), base='http://localhost:5199';
const wait=ms=>new Promise(r=>setTimeout(r,ms)),results=[];
const errorsFor=page=>{const errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});return errors;};
const overflow=page=>page.evaluate(()=>document.documentElement.scrollWidth-innerWidth);
const assets=path.resolve(__dirname,'../src/NexNovaCo.Web/wwwroot/image/project');
const sources=['nexconnect.jpg','payflowx.jpg','medilink.jpg','tradesync.jpg'];
const upload=async(field,index)=>{
 await field.locator('input[type=file]').setInputFiles({name:String.fromCharCode(65+index)+'.jpg',mimeType:'image/jpeg',buffer:fs.readFileSync(path.join(assets,sources[index]))});
 await field.locator('img').evaluate(img=>new Promise((resolve,reject)=>{if(img.complete&&img.naturalWidth)resolve();else{img.onload=resolve;img.onerror=reject}}));
 await field.locator('img[src^="data:"]').waitFor();
};
const save=async page=>{await page.getByRole('button',{name:'Save project',exact:true}).click();await page.getByRole('status').filter({hasText:'Project saved.'}).waitFor();await wait(300);};
const fields=page=>page.locator('.cms-image-field');
const galleryRows=page=>page.locator('section[aria-label="Project media"] .mud-paper').filter({has:page.getByLabel('Image alt text',{exact:true})});

async function editFlow(browser){
 const context=await browser.newContext({viewport:{width:1440,height:1000}});await context.addCookies(fixture.cookies);
 const page=await context.newPage(),errors=errorsFor(page);
 await page.goto(base+'/dashboard/content/shared-projects/1');await page.getByLabel('Name',{exact:true}).waitFor();await wait(800);
 const existing=await fields(page).locator('img').evaluateAll(imgs=>imgs.map(x=>x.getAttribute('src')));
 assert.equal(existing.length,3);assert.ok(existing.every(x=>x.startsWith('image/project/')));
 // A staged upload alone must warn; leaving discards it without a save.
 await upload(fields(page).first(),0);
 await page.getByRole('button',{name:'Cancel',exact:true}).click();await page.getByRole('button',{name:'Stay',exact:true}).click();
 assert.equal(await fields(page).first().locator('img[src^="data:"]').count(),1);
 await page.getByRole('button',{name:'Cancel',exact:true}).click();await page.getByRole('button',{name:'Leave',exact:true}).click();
 await page.waitForURL('**/dashboard/content/shared-projects');
 await page.goto(base+'/dashboard/content/shared-projects/1');await page.getByLabel('Name',{exact:true}).waitFor();await wait(700);
 assert.deepEqual(await fields(page).locator('img').evaluateAll(imgs=>imgs.map(x=>x.getAttribute('src'))),existing);
 while(await galleryRows(page).count()){
  const count=await galleryRows(page).count();await galleryRows(page).first().getByRole('button',{name:/Remove gallery image/}).click();
  await page.waitForFunction(count=>document.querySelectorAll('section[aria-label="Project media"] input[type=file]').length===count,count);
 }
 await upload(fields(page).first(),1);
 for(let i=0;i<4;i++){
  await page.getByRole('button',{name:'Add gallery image',exact:true}).click();
  const row=galleryRows(page).nth(i);await row.getByLabel('Image alt text',{exact:true}).fill('Project media '+String.fromCharCode(65+i));
  await upload(row.locator('.cms-image-field'),i);
 }
 // D,A,B,C — pending uploads must move with their keyed rows, not index-based component state.
 for(let index=3;index>0;index--){await galleryRows(page).nth(index).getByRole('button',{name:/Move up gallery image/}).click();await wait(200);}
 await save(page);await page.reload();await page.getByLabel('Name',{exact:true}).waitFor();await wait(700);
 const alts=await page.getByLabel('Image alt text',{exact:true}).evaluateAll(inputs=>inputs.map(x=>x.value));
 assert.deepEqual(alts,['Project media D','Project media A','Project media B','Project media C']);
 const images=await fields(page).locator('img').evaluateAll(imgs=>imgs.map(x=>x.getAttribute('src')));
 assert.ok(images.every(x=>/^uploads\/projects\/[a-f0-9]{32}\.jpg$/.test(x)));assert.notEqual(images[0],images[1]);
 for(const [index,source] of [1,3,0,1,2].entries()){
  const response=await context.request.get(base+'/'+images[index]);assert.equal(response.status(),200);
  assert.deepEqual(await response.body(),fs.readFileSync(path.join(assets,sources[source])));
 }
 fs.writeFileSync(out+'/saved.json',JSON.stringify({existing,cover:images[0],gallery:images.slice(1),alts}));
 // Dirty reorder and removal use the same guard.
 await galleryRows(page).first().getByRole('button',{name:/Move down gallery image/}).click();
 await page.getByRole('button',{name:'Cancel',exact:true}).click();await page.getByRole('button',{name:'Stay',exact:true}).click();
 await galleryRows(page).nth(1).getByRole('button',{name:/Move up gallery image/}).click();
 await galleryRows(page).last().getByRole('button',{name:/Remove gallery image/}).click();
 await page.getByRole('button',{name:'Cancel',exact:true}).click();await page.getByRole('button',{name:'Leave',exact:true}).click();
 await page.waitForURL('**/dashboard/content/shared-projects');
 // New Project upload-before-save with no temporary ID.
 await page.goto(base+'/dashboard/content/shared-projects/new');await page.getByLabel('Name',{exact:true}).waitFor();await wait(700);
 for(const [label,value] of Object.entries({Name:'Created media project',Slug:'created-media-project',Tagline:'Media create test',Summary:'Create workflow summary','Full description':'Full project content retained.'}))await page.getByLabel(label,{exact:true}).fill(value);
 await upload(fields(page).first(),0);await page.getByRole('button',{name:'Add gallery image',exact:true}).click();
 await galleryRows(page).first().getByLabel('Image alt text',{exact:true}).fill('Created gallery image');await upload(fields(page).nth(1),2);
 await save(page);await page.waitForURL(/shared-projects\/\d+$/);await page.reload();await page.getByLabel('Name',{exact:true}).waitFor();assert.equal(await page.getByLabel('Name',{exact:true}).inputValue(),'Created media project');
 assert.equal(await fields(page).locator('img[src^="uploads/projects/"]').count(),2);
 assert.deepEqual(errors,[]);results.push({type:'edit/create/dirty/upload/order',result:'PASS'});await context.close();console.log('CMS upload, order, dirty and create PASS');
}

async function verify(browser,channel){
 const saved=JSON.parse(fs.readFileSync(out+'/saved.json','utf8'));
 if(channel==='chrome'){
  for(const width of [1440,1024,768,390]){
   const context=await browser.newContext({viewport:{width,height:1000}});await context.addCookies(fixture.cookies);
   const page=await context.newPage(),errors=errorsFor(page);await page.goto(base+'/dashboard/content/shared-projects/1');await page.getByLabel('Name',{exact:true}).waitFor();await wait(700);
   assert.deepEqual(await page.getByLabel('Image alt text',{exact:true}).evaluateAll(xs=>xs.map(x=>x.value)),saved.alts);
   await fields(page).first().scrollIntoViewIfNeeded();assert.equal(await overflow(page),0);assert.deepEqual(errors,[]);
   await page.screenshot({path:out+'/editor-'+width+'.png',fullPage:true});await context.close();results.push({type:'editor',width,result:'PASS'});
  }
 }
 for(const width of channel==='chrome'?[1440,1201,1200,1024,768,390]:[1440,390]){
  const page=await browser.newPage({viewport:{width,height:1000}}),errors=errorsFor(page);
  await page.goto(base+'/projects/nexconnect');await page.waitForSelector('.project-detail-page[data-project-detail-enhanced]');
  const gallery=page.locator('.project-gallery');await gallery.scrollIntoViewIfNeeded();await wait(800);
  assert.deepEqual(await gallery.locator('img').evaluateAll(xs=>xs.map(x=>x.getAttribute('src'))),saved.gallery);
  assert.deepEqual(await gallery.locator('img').evaluateAll(xs=>xs.map(x=>x.alt)),saved.alts);
  assert.equal(await page.locator('.inner_project_content_hexagon').isVisible(),width>1200);
  assert.ok((await page.locator('meta[property="og:image"]').getAttribute('content')).endsWith(saved.cover));
  for(let i=0;i<4;i++){
   assert.equal(await gallery.locator('.carousel-item.active img').getAttribute('src'),saved.gallery[i]);
   assert.ok(await gallery.locator('.carousel-item.active img').evaluate(img=>img.complete&&img.naturalWidth>0));
   assert.equal(await gallery.locator('.carousel-item.active').evaluate(x=>getComputedStyle(x).opacity),'1');
   await gallery.getByRole('button',{name:'Next project image'}).click();await wait(650);
  }
  await gallery.getByRole('button',{name:'Previous project image'}).click();await wait(650);assert.equal(await gallery.locator('.carousel-item.active img').getAttribute('src'),saved.gallery[3]);
  await gallery.focus();await page.keyboard.press('ArrowRight');await wait(650);assert.equal(await gallery.locator('.carousel-item.active img').getAttribute('src'),saved.gallery[0]);
  await page.setViewportSize({width:width===390?1440:390,height:1000});await wait(500);assert.equal(await overflow(page),0);await page.setViewportSize({width,height:1000});
  await page.locator('.project-info-box').scrollIntoViewIfNeeded();await wait(1600);assert.equal(await overflow(page),0);
  await page.screenshot({path:out+'/'+channel+'-detail-'+width+'.png',fullPage:true});
  if(channel==='chrome')for(const route of ['/projects','/']){
   await page.goto(base+route);await wait(1200);const card=page.locator('.project_img img[src="'+saved.cover+'"]');await card.first().scrollIntoViewIfNeeded();await wait(1700);
   assert.ok(await card.first().evaluate(img=>img.complete&&img.naturalWidth>0));assert.equal(await overflow(page),0);
   if(route==='/projects')await page.screenshot({path:out+'/listing-'+width+'.png',fullPage:true});
  }
  await page.goto(base+'/projects/nexconnect');await page.waitForSelector('.project-detail-page[data-project-detail-enhanced]');await gallery.scrollIntoViewIfNeeded();await wait(600);await gallery.getByRole('button',{name:'Next project image'}).click();await wait(650);
  assert.equal(await gallery.locator('.carousel-item.active img').getAttribute('src'),saved.gallery[1]);assert.deepEqual(errors,[]);
  results.push({type:'public',channel,version:browser.version(),width,result:'PASS',overflow:0,errors});await page.close();console.log(channel,width,'PASS');
 }
}
async function states(browser){
 const context=await browser.newContext({viewport:{width:1440,height:1000}});await context.addCookies(fixture.cookies);
 const editor=await context.newPage(),page=await context.newPage(),errors=errorsFor(editor);const publicErrors=errorsFor(page);
 await editor.goto(base+'/dashboard/content/shared-projects/1');await editor.getByLabel('Name',{exact:true}).waitFor();await wait(700);
 const saved=JSON.parse(fs.readFileSync(out+'/saved.json','utf8'));
 assert.deepEqual(await fields(editor).locator('img').evaluateAll(xs=>xs.map(x=>x.getAttribute('src'))),[saved.cover,...saved.gallery]);
 assert.deepEqual(await editor.getByLabel('Image alt text',{exact:true}).evaluateAll(xs=>xs.map(x=>x.value)),saved.alts);
 await page.goto(base+'/projects/nexconnect');await page.waitForSelector('.project-detail-page[data-project-detail-enhanced]');
 assert.deepEqual(await page.locator('.project-gallery img').evaluateAll(xs=>xs.map(x=>x.getAttribute('src'))),saved.gallery);
 results.push({type:'restart-order-cover',result:'PASS'});console.log('restarted cover and four-image order PASS');
 for(const count of [3,5,2,1,0]){
  while(await galleryRows(editor).count()>count){const before=await galleryRows(editor).count();await galleryRows(editor).last().getByRole('button',{name:/Remove gallery image/}).click();await editor.waitForFunction(before=>document.querySelectorAll('section[aria-label="Project media"] input[type=file]').length===before,before);}
  while(await galleryRows(editor).count()<count){await editor.getByRole('button',{name:'Add gallery image',exact:true}).click();await wait(200);const row=galleryRows(editor).last();await row.getByLabel('Image alt text',{exact:true}).fill('Additional gallery');await upload(row.locator('.cms-image-field'),0);}
  await save(editor);await page.goto(base+'/projects/nexconnect');await page.waitForSelector('.project-detail-page[data-project-detail-enhanced]');
  const gallery=page.locator('.project-gallery');assert.equal(await gallery.count(),count?1:0);
  assert.equal(await gallery.locator('img').count(),count);assert.equal(await gallery.getByRole('button',{name:'Next project image'}).count(),count>1?1:0);
  if(count){await gallery.scrollIntoViewIfNeeded();await wait(500);assert.ok(await gallery.locator('img').first().evaluate(img=>img.complete&&img.naturalWidth>0));}
  assert.equal(await overflow(page),0);results.push({type:'gallery-state',count,result:'PASS'});console.log('gallery',count,'PASS');
 }
 await editor.reload();await editor.getByLabel('Name',{exact:true}).waitFor();assert.equal(await galleryRows(editor).count(),0);
 assert.deepEqual(errors,[]);assert.deepEqual(publicErrors,[]);await context.close();
}
async function emptyRestart(browser){
 const context=await browser.newContext();await context.addCookies(fixture.cookies);const page=await context.newPage(),errors=errorsFor(page);
 await page.goto(base+'/dashboard/content/shared-projects/1');await page.getByLabel('Name',{exact:true}).waitFor();await wait(700);assert.equal(await galleryRows(page).count(),0);
 const saved=JSON.parse(fs.readFileSync(out+'/saved.json','utf8'));
 assert.equal(await fields(page).first().locator('img').getAttribute('src'),saved.cover);
 for(const [index,source] of [1,3,0,1,2].entries()){
  const response=await context.request.get(base+'/'+[saved.cover,...saved.gallery][index]);assert.equal(response.status(),200);
  assert.deepEqual(await response.body(),fs.readFileSync(path.join(assets,sources[source])));
 }
 await page.goto(base+'/projects/nexconnect');await page.waitForSelector('.project-detail-page[data-project-detail-enhanced]');assert.equal(await page.locator('.project-gallery').count(),0);
 assert.ok((await page.locator('meta[property="og:image"]').getAttribute('content')).endsWith(saved.cover));
 await page.goto(base+'/projects/created-media-project');await page.waitForSelector('.project-detail-page[data-project-detail-enhanced]');assert.equal(await page.locator('.project-gallery img').count(),1);assert.equal(await page.getByRole('button',{name:'Next project image'}).count(),0);
 assert.deepEqual(errors,[]);results.push({type:'empty-restart-and-exact-upload-bytes',result:'PASS'});console.log('empty restart, retained cover, exact upload bytes and single-image create PASS');await context.close();
}
(async()=>{
 try{
  for(const channel of ['states','setup','empty'].includes(process.argv[4])?['chrome']:['chrome','msedge','moz-firefox']){
   const browser=channel==='moz-firefox'?await firefox.launch({channel,executablePath:'C:/Program Files/Mozilla Firefox/firefox.exe',headless:true}):await chromium.launch({channel,headless:true});
   try{
    if(process.argv[4]==='empty')await emptyRestart(browser);
    else if(process.argv[4]==='states')await states(browser);
    else{if(channel==='chrome'&&process.argv[4]!=='verify')await editFlow(browser);if(process.argv[4]!=='setup')await verify(browser,channel);}
   }finally{await browser.close();}
  }
 }finally{fs.writeFileSync(out+'/'+(process.argv[4]||'workflow')+'-results.json',JSON.stringify(results,null,2));}
})().catch(e=>{console.error(e);process.exitCode=1});

import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {spawnSync} from 'node:child_process';

const target=process.argv[2];
if(!target){console.error('Usage: node check-html-module.mjs <html>');process.exit(2)}
const html=fs.readFileSync(target,'utf8');
const matches=[...html.matchAll(/<script\s+type=["']module["'][^>]*>([\s\S]*?)<\/script>/gi)];
if(matches.length!==1){
  console.error(`Expected exactly one module script, found ${matches.length}`);
  process.exit(1);
}
const body=matches[0][1];
const tmp=path.join(os.tmpdir(),'highfly-interior-editor-smoke.mjs');
fs.writeFileSync(tmp,body);
const check=spawnSync(process.execPath,['--check',tmp],{stdio:'inherit'});
if(check.status!==0) process.exit(check.status??1);
const duplicateSymbols=['thumbQueue','hfThumbQueueV5'];
for(const symbol of duplicateSymbols){
  const re=new RegExp('(?:const|let|var)\\s+'+symbol+'\\b','g');
  const n=(body.match(re)||[]).length;
  if(n>1){console.error(`Duplicate declaration: ${symbol} x${n}`);process.exit(1)}
}
console.log(`HTML module syntax PASS: ${target}`);

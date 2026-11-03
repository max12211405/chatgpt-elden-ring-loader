const fs=require('node:fs'),path=require('node:path');
const artifacts=path.resolve(__dirname,'../../artifacts');fs.mkdirSync(artifacts,{recursive:true});
function testTone(){
 const sampleRate=8000, samples=sampleRate*18, pcm=Buffer.alloc(samples*2),header=Buffer.alloc(44);
 for(let i=0;i<samples;i++)pcm.writeInt16LE(Math.round(Math.sin(2*Math.PI*220*i/sampleRate)*800),i*2);
 header.write('RIFF');header.writeUInt32LE(36+pcm.length,4);header.write('WAVEfmt ',8);header.writeUInt32LE(16,16);header.writeUInt16LE(1,20);header.writeUInt16LE(1,22);header.writeUInt32LE(sampleRate,24);header.writeUInt32LE(sampleRate*2,28);header.writeUInt16LE(2,32);header.writeUInt16LE(16,34);header.write('data',36);header.writeUInt32LE(pcm.length,40);
 const file=path.join(artifacts,'generated-test-tone.wav');fs.writeFileSync(file,Buffer.concat([header,pcm]));return file;
}
module.exports={artifacts,testTone};

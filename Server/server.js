const domain = "www.abcdefg.com";
const express = require('express');
const https = require('https');
const fs = require('fs');
const util = require('util');
const stat = util.promisify(fs.stat);
const bodyParser = require('body-parser');
const path = require('path');
const cors = require('cors');

//TLS인증서
const ssl_options = {
  ca: fs.readFileSync(`/etc/letsencrypt/live/${domain}/fullchain.pem`),
  key: fs.readFileSync(`/etc/letsencrypt/live/${domain}/privkey.pem`),
  cert: fs.readFileSync(`/etc/letsencrypt/live/${domain}/cert.pem`)
};
const app = express();
//WebGL에서 접근 하기 위해서서
const corsOptions = {
  origin: function (origin, callback) {
    // 모든 origin 허용
    callback(null, true);
  }
};

app.use(cors(corsOptions));
app.use(bodyParser.json());
app.use(express.json({ limit: '500mb' }));
// 데이터 저장 요청 처리
app.post('/saveData', (req, res) => {
  // 클라이언트로부터 받은 파일명과 파일데이터
  const fileName = req.body.fileName;
  const fileData = req.body.fileData;

  if (!fileName || !fileData) {
      res.status(400).send('파일명과 파일데이터를 모두 제공해주세요.');
      return;
  }

  // 파일 경로 설정
  const filePath = path.join("./", 'Room', fileName);

  // 파일 쓰기
  fs.writeFile(filePath, fileData, (err) => {
      if (err) {
          console.error('파일을 저장하는 중 오류가 발생했습니다.', err);
          res.status(500).send('파일 저장에 실패했습니다.');
          return;
      }
      console.log('파일이 성공적으로 저장되었습니다.');
      res.status(200).send('파일이 성공적으로 저장되었습니다.');
  });
});

// 데이터 읽기 요청 처리
app.get('/readlist', async (req, res) => {
  // './Room' 폴더 경로 설정
  const roomFolderPath = './Room';
  
  try {
      // './Room' 폴더 내의 파일 목록 가져오기
      const files = await fs.promises.readdir(roomFolderPath);

      // 파일명과 파일 크기를 조합하여 파일 정보 배열 생성
      const fileInfo = [];
      for (const file of files) {
          const filePath = path.join(roomFolderPath, file);
          const fileStat = await stat(filePath);
          const fileSizeMB = Math.floor(fileStat.size / (1024 * 1024)); // 파일 크기를 MB, 소수점버림
          fileInfo.push(`${file} : ${fileSizeMB}`);
      }

      // 클라이언트에게 파일 정보 배열 응답
      res.status(200).json(fileInfo);
  } catch (err) {
      console.error('폴더 내 파일 목록을 가져오는 중 오류가 발생했습니다.', err);
      res.status(500).send('파일 목록을 가져오는 데 실패했습니다.');
  }
});

app.get('/Room/:fileName', (req, res) => {
  // 클라이언트로부터 요청한 파일명
  const fileName = req.params.fileName;

  if (!fileName) {
      res.status(400).send('파일명을 제공해주세요.');
      return;
  }

  // 파일 경로 설정
  const filePath = path.join("./", 'Room', fileName);

  // 파일 존재 여부 확인
  fs.access(filePath, fs.constants.F_OK, (err) => {
      if (err) {
          console.error('파일을 찾을 수 없습니다.', err);
          res.status(404).send('파일을 찾을 수 없습니다.');
          return;
      }

      // 스트림 생성 및 전송
      const stream = fs.createReadStream(filePath);
      stream.pipe(res);
  });
});

app.get('/maplist', async (req, res) => {
  // './Room' 폴더 경로 설정
  const mapFolderPath = './RoomLightMap';

  try {
      // './Room' 폴더 내의 파일 목록 가져오기
      const files = await fs.promises.readdir(mapFolderPath);

      // 파일명과 파일 크기를 조합하여 파일 정보 배열 생성
      const fileInfo = [];
      for (const file of files) {
        if (!path.extname(file)) {
          fileInfo.push(file);
        }
      }
      res.status(200).json(fileInfo);
  } catch (err) {
      console.error('폴더 내 파일 목록을 가져오는 중 오류가 발생했습니다.', err);
      res.status(500).send('파일 목록을 가져오는 데 실패했습니다.');
  }
});

app.get('/RoomLightMap/:fileName', (req, res) => {
  // 클라이언트로부터 요청한 파일명
  const fileName = req.params.fileName;

  if (!fileName) {
      res.status(400).send('파일명을 제공해주세요.');
      return;
  }

  // 파일 경로 설정
  const filePath = path.join("./", 'RoomLightMap', fileName);

  // 파일 존재 여부 확인
  fs.access(filePath, fs.constants.F_OK, (err) => {
      if (err) {
          console.error('파일을 찾을 수 없습니다.', err);
          res.status(404).send('파일을 찾을 수 없습니다.');
          return;
      }

      // 스트림 생성 및 전송
      const stream = fs.createReadStream(filePath);
      stream.pipe(res);
  });
});

// 클라이언트로부터 파일 데이터를 POST로 받음
app.post('/upload/:fileName', (req, res) => {
  const fileName = req.params.fileName;
  if (!fileName) {
      res.status(400).send('파일명을 제공해주세요.');
      return;
  }

  const filePath = path.join("./", "Room", fileName);
  const writeStream = fs.createWriteStream(filePath);

  req.pipe(writeStream);

  writeStream.on('finish', () => {
      console.log('파일이 성공적으로 저장되었습니다:', fileName);
      res.status(200).send('파일이 성공적으로 저장되었습니다.');
  });

  writeStream.on('error', (err) => {
      console.error('파일 쓰기 중 오류 발생:', err);
      res.status(500).send('파일 쓰기 중 오류가 발생했습니다.');
  });
});

const PORT = 8000;
https.createServer(ssl_options, app).listen(PORT, () => {
  console.log(`서버가 포트 ${PORT}에서 실행 중입니다.0327`);
  const roomFolderPath = path.join("./", 'Room');
    if (!fs.existsSync(roomFolderPath)) {
        fs.mkdirSync(roomFolderPath);
        console.log('Room 폴더가 생성되었습니다.');
    }
});

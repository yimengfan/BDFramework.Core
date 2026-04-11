"""抓取固定分支构建日志里的上传关键字，用于快速核对 TeamCity 上传行为。"""

import argparse
import collections
import json
import time
import urllib.error
import urllib.parse
import urllib.request

import update_project_settings as ups

BRANCH = 'v4/v-4.0.0'
BUILD_TYPES = ['BDFrameworkCore_BuildClientPackageIos', 'BDFrameworkCore_BuildClientPackageWindows']
KEYWORDS = ['uploadPreparedSource', 'uploadProgress', 'uploadedFiles', 'uploadRemoteRoot', 'uploadFileCount', '.zip', '不要发布', 'DoNotShip', 'BurstDebugInformation_DoNotShip']
FIELDS = 'id,buildTypeId,number,state,status,statusText,branchName,webUrl,queuedDate,startDate,finishDate,agent(id,name)'
args = argparse.Namespace(env_file=str(ups.DEFAULT_ENV_FILE), base_url=None, project_id=None)
config = ups.build_config(args)

def get_json(path,retries=5):
    for attempt in range(1,retries+1):
        req=urllib.request.Request(url=f'{config.base_url}{path}',method='GET',headers=ups.build_headers(config))
        try:
            with urllib.request.urlopen(req) as resp:
                raw=resp.read().decode('utf-8','replace')
                return json.loads(raw) if raw else {}
        except urllib.error.HTTPError as exc:
            _=exc.read().decode('utf-8','replace')
            if exc.code==502 and attempt<retries:
                time.sleep(attempt); continue
            raise
        except urllib.error.URLError:
            if attempt<retries:
                time.sleep(attempt); continue
            raise

def stream_log(build_id,retries=5):
    path=f'/downloadBuildLog.html?buildId={build_id}'
    for attempt in range(1,retries+1):
        req=urllib.request.Request(url=f'{config.base_url}{path}',method='GET',headers=ups.build_headers(config))
        try:
            matches=[]; tail=collections.deque(maxlen=40)
            with urllib.request.urlopen(req) as resp:
                for line_no,raw in enumerate(resp,1):
                    line=raw.decode('utf-8','replace').rstrip('\r\n')
                    tail.append((line_no,line))
                    if any(k in line for k in KEYWORDS):
                        matches.append((line_no,line))
            return matches,list(tail)
        except urllib.error.HTTPError as exc:
            _=exc.read().decode('utf-8','replace')
            if exc.code==502 and attempt<retries:
                time.sleep(attempt); continue
            raise
        except urllib.error.URLError:
            if attempt<retries:
                time.sleep(attempt); continue
            raise

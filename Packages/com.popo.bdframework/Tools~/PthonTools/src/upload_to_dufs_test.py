import http.server
import socketserver
import threading
import pytest

from src import upload_to_dufs


# Unit tests for run_upload function

class TestRunUpload:

    testServer = "http://192.168.0.240:20000/"

    def test_upload_single_file_success(self, tmp_path):
        """Test uploading a single file successfully."""
        local = tmp_path / "single.txt"
        local.write_text("content3")
        exit_code = upload_to_dufs.run_upload(
            source=local,
            remote_dir="/uploads",
            endpoint=self.testServer,
        )
        assert exit_code == 0



    def test_upload_directory_success(self, tmp_path):
        """Test uploading a directory successfully."""
        nested = tmp_path / "nested"
        (nested / "sub").mkdir(parents=True)
        (nested / "a.txt").write_text("a")
        (nested / "sub" / "b.txt").write_text("b")
        exit_code = upload_to_dufs.run_upload(
            source=nested,
            remote_dir="/remote",
            endpoint=self.testServer,
        )
        assert exit_code == 0



    def test_upload_empty_directory_no_calls(self, tmp_path):
        """Test uploading an empty directory."""
        empty_dir = tmp_path / "empty"
        empty_dir.mkdir()
        exit_code = upload_to_dufs.run_upload(
            source=empty_dir,
            remote_dir="/remote",
            endpoint=self.testServer,
        )
        assert exit_code == 0



    def test_upload_nonexistent_path_returns_error(self, tmp_path):
        """Test uploading a nonexistent file."""
        missing = tmp_path / "missing.txt"
        exit_code = upload_to_dufs.run_upload(
            source=missing,
            remote_dir="/remote",
        )
        assert exit_code == 1


    def test_upload_same_file_twice_overrides(self, tmp_path):
        local = tmp_path / "dup.txt"
        local.write_text("v1")
        assert upload_to_dufs.run_upload(
            source=local,
            remote_dir="/uploads",
            endpoint=self.testServer,
        ) == 0
        local.write_text("v2")
        assert upload_to_dufs.run_upload(
            source=local,
            remote_dir="/uploads",
            endpoint=self.testServer,
        ) == 0


if __name__ == '__main__':
    pytest.main([__file__, '-v'])
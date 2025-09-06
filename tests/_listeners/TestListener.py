"""
Custom Robot Framework listener for GitBackup tests
Provides additional logging and test execution monitoring
"""

import os
import sys
from datetime import datetime
from robot.libraries.BuiltIn import BuiltIn


class TestListener:
    """Custom test listener for GitBackup test suite"""
    
    ROBOT_LISTENER_API_VERSION = 3
    
    def __init__(self, log_file='listener.log'):
        """Initialize the test listener"""
        self.log_file = log_file
        self.test_count = 0
        self.passed_count = 0
        self.failed_count = 0
        self.start_time = None
        
        # Ensure log directory exists
        os.makedirs(os.path.dirname(log_file) if os.path.dirname(log_file) else '.', exist_ok=True)
        
        # Initialize log file
        with open(self.log_file, 'w') as f:
            f.write(f"GitBackup Test Execution Log\n")
            f.write(f"Started: {datetime.now().isoformat()}\n")
            f.write(f"Platform: {sys.platform}\n")
            f.write("=" * 50 + "\n\n")
    
    def start_suite(self, data, result):
        """Called when a test suite starts"""
        self.log(f"Suite started: {data.name}")
        if data.doc:
            self.log(f"Documentation: {data.doc}")
        self.start_time = datetime.now()
    
    def end_suite(self, data, result):
        """Called when a test suite ends"""
        duration = datetime.now() - self.start_time if self.start_time else "Unknown"
        self.log(f"Suite ended: {data.name}")
        self.log(f"Duration: {duration}")
        self.log(f"Tests: {self.test_count}, Passed: {self.passed_count}, Failed: {self.failed_count}")
        self.log("-" * 30)
    
    def start_test(self, data, result):
        """Called when a test case starts"""
        self.test_count += 1
        self.log(f"Test {self.test_count}: {data.name}")
        if data.doc:
            self.log(f"  Doc: {data.doc}")
    
    def end_test(self, data, result):
        """Called when a test case ends"""
        if result.passed:
            self.passed_count += 1
            status = "PASS"
        else:
            self.failed_count += 1
            status = "FAIL"
            self.log(f"  Error: {result.message}")
        
        self.log(f"  Status: {status}")
        self.log(f"  Duration: {result.elapsed_time}ms")
        
    def start_keyword(self, data, result):
        """Called when a keyword starts (optional, for debugging)"""
        pass
    
    def end_keyword(self, data, result):
        """Called when a keyword ends (optional, for debugging)"""
        if result.status == 'FAIL' and data.type == 'KEYWORD':
            self.log(f"  Keyword failed: {data.kwname} - {result.message}")
    
    def log_message(self, message):
        """Called when a log message is written"""
        if message.level in ('ERROR', 'WARN'):
            self.log(f"  {message.level}: {message.message}")
    
    def log(self, message):
        """Write message to log file"""
        with open(self.log_file, 'a') as f:
            timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            f.write(f"[{timestamp}] {message}\n")
    
    def close(self):
        """Called when listener is being closed"""
        with open(self.log_file, 'a') as f:
            f.write(f"\nTest execution completed: {datetime.now().isoformat()}\n")
            f.write(f"Final results - Total: {self.test_count}, Passed: {self.passed_count}, Failed: {self.failed_count}\n")

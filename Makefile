NAME=RyzenTuner
VERSION=$(shell git describe --tags --abbrev=0 || echo "unknown version")
BUILD_TIME=$(shell date "+%Y%m%d%H%M%S")

RELEASE_FILE_NAME=$(shell echo $(NAME)-$(VERSION)-$(BUILD_TIME).zip)

release:
	rm -rf bin/tmp
	cp -r bin/Release bin/tmp
	cd bin/tmp && rm -f *.xml *.log *.pdb *.stackdump && rm -rf tmp
	cd bin/tmp && sed -i 's#<value>Debug</value>#<value>Warning</value>#g' RyzenTuner.exe.config
	cd bin/tmp && zip -r -9 $(RELEASE_FILE_NAME) ./*
	mv bin/tmp/$(RELEASE_FILE_NAME) bin/
	rm -rf bin/tmp

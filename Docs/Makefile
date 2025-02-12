# Makefile for markdown html files
PANDOC  = pandoc
FLAGS  ?=  --from=gfm --to=html5+smart --css styling.css -s --mathjax

SOURCES = paper.md
HEADERS = styling.css

OUTPUTS = $(patsubst %.md,%.html, $(filter %.md,$(SOURCES)))

.PHONY: all clean help vars

all: $(OUTPUTS)

%.html: %.md $(HEADERS)
	$(PANDOC) $(FLAGS) -o $@ $<

RM = rm
clean:
	$(RM) $(OUTPUTS)

# print variables
vars:
	@echo "PANDOC                   $(PANDOC)"
	@echo "FLAGS                    $(FLAGS)"
	@echo "SOURCES                  $(SOURCES)"
	@echo "OUTPUTS                  $(OUTPUTS)"

# print help
help:
	@echo "usage: make [FLAGS=<flags>]"
	@echo "make all        build all html files from corresponding markdown files"
	@echo "make help       show this help message"
	@echo "make vars       preview what sources and outputs are defined"
	@echo "make file.html  builds specified html file from corresponding markdown file"
	@echo "make clean      deletes all html files that have a corresponding md file"

\ kb.fs — Kanban CLI tool in GForth
\ Text-first, methodology-agnostic task tracking

\ ============================================================
\ SECTION 1: Configuration and Constants
\ ============================================================

256 constant MAX-ITEMS
256 constant MAX-STRING
64  constant MAX-LANES
32  constant MAX-TAGS-PER-ITEM
32  constant MAX-DEPS-PER-ITEM

\ ============================================================
\ SECTION 2: String Utilities
\ ============================================================

: str-alloc ( u -- addr )
    \ Allocate u bytes from heap
    allocate throw ;

: str-free ( addr -- )
    free throw ;

: str-copy ( c-addr u -- addr u )
    \ Copy string to newly allocated memory
    dup str-alloc          ( c-addr u new-addr )
    dup >r                 ( c-addr u new-addr ) ( R: new-addr )
    swap dup >r            ( c-addr new-addr u ) ( R: new-addr u )
    move
    r> r> swap ;           ( new-addr u )

: str-equal? ( c-addr1 u1 c-addr2 u2 -- flag )
    \ Check if lengths are equal first
    2 pick over <> if 2drop 2drop false exit then
    compare 0= ;

: str-prefix? ( c-addr1 u1 c-addr2 u2 -- flag )
    \ Is (c-addr2 u2) a prefix of (c-addr1 u1)?
    \ First, check if string1 is long enough
    2over 2over         \ c-addr1 u1 c-addr2 u2 c-addr1 u1 c-addr2 u2
    2drop nip           \ c-addr1 u1 c-addr2 u2 u1
    over < if           \ u1 < u2?
        2drop 2drop false exit
    then
    \ Stack: c-addr1 u1 c-addr2 u2
    \ Compare first u2 bytes of string1 with string2
    \ We need: c-addr1 u2 c-addr2 u2
    tuck                \ c-addr1 u1 u2 c-addr2 u2
    2>r                 \ c-addr1 u1 u2   R: c-addr2 u2
    nip                 \ c-addr1 u2      R: c-addr2 u2
    2r>                 \ c-addr1 u2 c-addr2 u2
    compare 0= ;

: skip-spaces ( c-addr u -- c-addr' u' )
    begin
        dup 0> while
        over c@ bl = while
        1 /string
    repeat then ;

: skip-to-space ( c-addr u -- c-addr' u' )
    begin
        dup 0> while
        over c@ bl <> while
        1 /string
    repeat then ;

: parse-word-from ( c-addr u -- rest-addr rest-len word-addr word-len )
    \ Skip leading spaces
    skip-spaces
    dup 0= if
        \ Empty string - return empty word
        2dup exit  \ return (addr 0 addr 0)
    then
    \ Save start position
    over >r                 \ R: word-start
    \ Find end of word
    skip-to-space           \ rest-addr rest-len
    \ Compute word length: original position - rest position
    over r@ -               \ rest-addr rest-len word-len
    r> swap ;               \ rest-addr rest-len word-addr word-len

\ Temporary string buffer for parsing
create temp-buf MAX-STRING allot

: temp-str ( c-addr u -- c-addr' u )
    \ Copy string to temp buffer (for temporary use)
    dup MAX-STRING 1- min >r
    temp-buf r@ move
    temp-buf r> ;

\ ============================================================
\ SECTION 3: Dynamic Array for Lanes
\ ============================================================

create lanes MAX-LANES cells allot
create lane-lens MAX-LANES cells allot
variable lane-count

: lane-init ( -- )
    0 lane-count ! ;

: lane-add ( c-addr u -- )
    lane-count @ MAX-LANES >= if 2drop exit then
    str-copy                          \ ( new-addr u )
    swap                              \ ( u new-addr )
    lane-count @ cells lanes + !      \ store new-addr to lanes[i], stack: ( u )
    lane-count @ cells lane-lens + !  \ store u to lane-lens[i]
    1 lane-count +! ;

: lane@ ( n -- c-addr u )
    dup cells lanes + @
    swap cells lane-lens + @ ;

: lane-find ( c-addr u -- n|-1 )
    lane-count @ 0 ?do
        2dup i lane@ str-equal? if 2drop i unloop exit then
    loop
    2drop -1 ;

: default-lanes ( -- )
    s" backlog" lane-add
    s" doing" lane-add
    s" review" lane-add
    s" done" lane-add ;

\ ============================================================
\ SECTION 4: Item Structure
\ ============================================================

\ Item structure offsets (using cells for simplicity)
\ 0: id-addr, 1: id-len
\ 2: title-addr, 3: title-len
\ 4: type-addr, 5: type-len
\ 6: status-addr, 7: status-len
\ 8: priority-addr, 9: priority-len
\ 10: desc-addr, 11: desc-len
\ 12: assignee-addr, 13: assignee-len
\ 14: created-addr, 15: created-len
\ 16: tags-count
\ 17-48: tags (16 pairs of addr/len)
\ 49: deps-count
\ 50-81: deps (16 pairs of addr/len)

32 constant TAGS-OFFSET
17 constant TAGS-COUNT-OFFSET
50 constant DEPS-COUNT-OFFSET
51 constant DEPS-OFFSET
83 constant ITEM-SIZE  \ total cells per item

create items MAX-ITEMS ITEM-SIZE cells * allot
variable item-count

: item-addr ( n -- addr )
    ITEM-SIZE cells * items + ;

: item-init ( -- )
    0 item-count !
    items MAX-ITEMS ITEM-SIZE cells * 0 fill ;

: item-field! ( c-addr u item-addr field-offset -- )
    cells + >r
    str-copy
    r@ cell+ !  \ store length
    r> ! ;      \ store address

: item-field@ ( item-addr field-offset -- c-addr u )
    cells + dup @
    swap cell+ @ ;

\ Specific field accessors
: item-id! ( c-addr u item-addr -- ) 0 item-field! ;
: item-id@ ( item-addr -- c-addr u ) 0 item-field@ ;

: item-title! ( c-addr u item-addr -- ) 2 item-field! ;
: item-title@ ( item-addr -- c-addr u ) 2 item-field@ ;

: item-type! ( c-addr u item-addr -- ) 4 item-field! ;
: item-type@ ( item-addr -- c-addr u ) 4 item-field@ ;

: item-status! ( c-addr u item-addr -- ) 6 item-field! ;
: item-status@ ( item-addr -- c-addr u ) 6 item-field@ ;

: item-priority! ( c-addr u item-addr -- ) 8 item-field! ;
: item-priority@ ( item-addr -- c-addr u ) 8 item-field@ ;

: item-desc! ( c-addr u item-addr -- ) 10 item-field! ;
: item-desc@ ( item-addr -- c-addr u ) 10 item-field@ ;

: item-assignee! ( c-addr u item-addr -- ) 12 item-field! ;
: item-assignee@ ( item-addr -- c-addr u ) 12 item-field@ ;

: item-created! ( c-addr u item-addr -- ) 14 item-field! ;
: item-created@ ( item-addr -- c-addr u ) 14 item-field@ ;

: item-tags-count@ ( item-addr -- n )
    TAGS-COUNT-OFFSET cells + @ ;

: item-tags-count! ( n item-addr -- )
    TAGS-COUNT-OFFSET cells + ! ;

: item-tag@ ( item-addr n -- c-addr u )
    2* TAGS-OFFSET + cells + dup @
    swap cell+ @ ;

: item-tag! ( c-addr u item-addr n -- )
    2* TAGS-OFFSET + cells + >r
    str-copy
    r@ cell+ !
    r> ! ;

: item-add-tag ( c-addr u item-addr -- )
    dup item-tags-count@ MAX-TAGS-PER-ITEM >= if drop 2drop exit then
    dup >r                      \ c-addr u item-addr, R: item-addr
    item-tags-count@            \ c-addr u n
    r@ swap                     \ c-addr u item-addr n
    item-tag!
    r@ item-tags-count@ 1+ r> item-tags-count! ;

: item-deps-count@ ( item-addr -- n )
    DEPS-COUNT-OFFSET cells + @ ;

: item-deps-count! ( n item-addr -- )
    DEPS-COUNT-OFFSET cells + ! ;

: item-dep@ ( item-addr n -- c-addr u )
    2* DEPS-OFFSET + cells + dup @
    swap cell+ @ ;

: item-dep! ( c-addr u item-addr n -- )
    2* DEPS-OFFSET + cells + >r
    str-copy
    r@ cell+ !
    r> ! ;

: item-add-dep ( c-addr u item-addr -- )
    dup item-deps-count@ MAX-DEPS-PER-ITEM >= if drop 2drop exit then
    \ Stack: c-addr u item-addr
    \ Need: c-addr u item-addr n for item-dep!
    dup >r                      \ c-addr u item-addr, R: item-addr
    item-deps-count@            \ c-addr u n
    r@ swap                     \ c-addr u item-addr n
    item-dep!
    r@ item-deps-count@ 1+ r> item-deps-count! ;

\ ============================================================
\ SECTION 5: Item Management
\ ============================================================

: new-item ( -- item-addr )
    item-count @ MAX-ITEMS >= if 0 exit then
    item-count @ item-addr
    dup ITEM-SIZE cells 0 fill
    1 item-count +! ;

: find-item-by-id ( c-addr u -- item-addr | 0 )
    item-count @ 0 ?do
        2dup i item-addr item-id@ str-equal? if
            2drop i item-addr unloop exit
        then
    loop
    2drop 0 ;

\ ID generation
variable next-id
: id-init ( -- ) 1 next-id ! ;

create id-buf 16 allot

: generate-id ( -- c-addr u )
    s" KAN-" id-buf swap move
    next-id @ s>d <# # # # #> id-buf 4 + swap move
    1 next-id +!
    \ Fixed length: KAN- (4) + 3 digits = 7
    id-buf 7 ;

\ Update next-id based on existing items
: update-next-id ( -- )
    item-count @ 0 ?do
        i item-addr item-id@  ( c-addr u )
        \ Check if starts with KAN-
        dup 4 >= if
            over 4 s" KAN-" str-equal? if
                \ Extract number after KAN-
                4 /string  ( c-addr' u' )
                0 -rot     ( 0 c-addr' u' )
                bounds ?do
                    i c@ [char] 0 - swap 10 * +
                loop
                1+ next-id @ max next-id !
            else
                2drop
            then
        else
            2drop
        then
    loop ;

\ ============================================================
\ SECTION 6: Parser - Forth-native executable format
\ ============================================================

\ The data format is executable Forth:
\
\ board: default
\ lanes: backlog doing review done
\
\ item[ KAN-001
\   type: spike
\   title: Evaluate Smalltalk
\   status: backlog
\   tags: research language-spike
\   priority: p1
\   desc: |
\     Multi-line description here.
\     More lines.
\   |
\ ]item

variable parsing-item
variable parse-line-addr
variable parse-line-len

: set-parse-line ( c-addr u -- )
    parse-line-len ! parse-line-addr ! ;

: get-parse-line ( -- c-addr u )
    parse-line-addr @ parse-line-len @ ;

: trim-leading ( c-addr u -- c-addr' u' )
    begin
        dup 0> while
        over c@ bl <= while
        1 /string
    repeat then ;

: trim-trailing ( c-addr u -- c-addr u' )
    begin
        dup 0> while
        2dup + 1- c@ bl <= while
        1-
    repeat then ;

: trim ( c-addr u -- c-addr' u' )
    trim-leading trim-trailing ;

: skip-colon ( c-addr u -- c-addr' u' )
    \ Skip leading colon and space if present
    dup 0> if
        over c@ [char] : = if
            1 /string trim-leading
        then
    then ;

: parse-field-name ( c-addr u -- c-addr' u' name-addr name-len )
    \ Find the colon and extract the field name
    2dup [char] : scan  ( orig-addr orig-len colon-addr remaining )
    dup 0= if
        2drop s" " exit
    then
    drop over -         ( orig-addr orig-len name-len )
    >r 2dup r@ /string  ( orig-addr orig-len orig-addr name-len )
    2swap 2drop         ( orig-addr name-len )
    r> drop
    2dup + 1+ -rot      ( rest-addr orig-addr name-len )
    -rot swap           ( name-len rest-addr name-addr )
    -rot                ( name-addr name-len rest-addr )
    \ Now skip past colon
    parse-line-addr @ parse-line-len @ rot - swap drop
    swap >r             ( name-addr ) ( R: name-len )
    parse-line-addr @ parse-line-len @
    r@ /string 1 /string trim-leading   ( name-addr rest-addr rest-len )
    2swap drop r> ;     ( rest-addr rest-len name-addr name-len )

: handle-board ( c-addr u -- )
    \ Just skip board name for now, we use default
    2drop ;

: handle-lanes ( c-addr u -- )
    \ Parse space-separated lane names
    lane-count @ 0= if
        begin
            dup 0> while
            parse-word-from  ( rest-addr rest-len word-addr word-len )
            dup 0> if lane-add else 2drop then
        repeat
    then
    2drop ;

: handle-item-start ( c-addr u -- )
    \ c-addr u is the item ID
    new-item parsing-item !
    parsing-item @ 0= if 2drop exit then
    parsing-item @ item-id! ;

: handle-type ( c-addr u -- )
    parsing-item @ 0= if 2drop exit then
    parsing-item @ item-type! ;

: handle-title ( c-addr u -- )
    parsing-item @ 0= if 2drop exit then
    parsing-item @ item-title! ;

: handle-status ( c-addr u -- )
    parsing-item @ 0= if 2drop exit then
    parsing-item @ item-status! ;

: handle-priority ( c-addr u -- )
    parsing-item @ 0= if 2drop exit then
    parsing-item @ item-priority! ;

: handle-assignee ( c-addr u -- )
    parsing-item @ 0= if 2drop exit then
    parsing-item @ item-assignee! ;

: handle-created ( c-addr u -- )
    parsing-item @ 0= if 2drop exit then
    parsing-item @ item-created! ;

: handle-tags ( c-addr u -- )
    parsing-item @ 0= if 2drop exit then
    begin
        dup 0> while
        parse-word-from  ( rest-addr rest-len word-addr word-len )
        dup 0> if parsing-item @ item-add-tag else 2drop then
    repeat
    2drop ;

: handle-deps ( c-addr u -- )
    parsing-item @ 0= if 2drop exit then
    begin
        dup 0> while
        parse-word-from
        dup 0> if parsing-item @ item-add-dep else 2drop then
    repeat
    2drop ;

: handle-desc ( c-addr u -- )
    parsing-item @ 0= if 2drop exit then
    \ Simple single-line for now
    parsing-item @ item-desc! ;

: handle-item-end ( -- )
    0 parsing-item ! ;

: parse-line ( c-addr u -- )
    trim
    dup 0= if 2drop exit then           \ empty line
    over c@ [char] # = if 2drop exit then  \ comment

    \ Check for item[ start
    2dup s" item[" str-prefix? if
        5 /string trim
        handle-item-start exit
    then

    \ Check for ]item end
    2dup s" ]item" str-equal? if
        2drop handle-item-end exit
    then

    \ Check for board:
    2dup s" board:" str-prefix? if
        6 /string trim handle-board exit
    then

    \ Check for lanes:
    2dup s" lanes:" str-prefix? if
        6 /string trim handle-lanes exit
    then

    \ Inside item, parse field: value
    parsing-item @ 0= if 2drop exit then

    2dup s" type:" str-prefix? if
        5 /string trim handle-type exit
    then

    2dup s" title:" str-prefix? if
        6 /string trim handle-title exit
    then

    2dup s" status:" str-prefix? if
        7 /string trim handle-status exit
    then

    2dup s" priority:" str-prefix? if
        9 /string trim handle-priority exit
    then

    2dup s" tags:" str-prefix? if
        5 /string trim handle-tags exit
    then

    2dup s" deps:" str-prefix? if
        5 /string trim handle-deps exit
    then

    2dup s" assignee:" str-prefix? if
        9 /string trim handle-assignee exit
    then

    2dup s" created:" str-prefix? if
        8 /string trim handle-created exit
    then

    2dup s" desc:" str-prefix? if
        5 /string trim handle-desc exit
    then

    2drop ;

\ File reading
256 constant LINE-BUF-SIZE
create line-buf LINE-BUF-SIZE allot

: read-line-safe ( fileid -- c-addr u flag | 0 0 false )
    line-buf LINE-BUF-SIZE rot read-line throw
    if line-buf swap true
    else drop 0 0 false
    then ;

variable data-file-id

: load-data ( c-addr u -- flag )
    r/o open-file if drop false exit then
    data-file-id !
    begin
        data-file-id @ read-line-safe
    while
        parse-line
    repeat
    2drop
    data-file-id @ close-file drop
    update-next-id
    true ;

\ Default data file
: default-data-file ( -- c-addr u )
    s" kb.dat" ;

: try-load-data ( -- )
    default-data-file load-data drop ;

\ ============================================================
\ SECTION 7: Serializer
\ ============================================================

: write-string ( c-addr u fileid -- )
    2 pick 0= if drop 2drop exit then  \ skip null address
    over 0= if drop 2drop exit then    \ skip empty length
    -rot rot write-file throw ;

: write-line ( c-addr u fileid -- )
    dup >r write-string
    s\" \n" r> write-string ;

: write-indent ( fileid -- )
    s"   " rot write-string ;

variable save-fid

\ save-item uses save-fid variable (set by save-data)
: save-item ( item-addr -- )
    s" item[ " save-fid @ write-string
    dup item-id@ save-fid @ write-line

    save-fid @ write-indent s" type: " save-fid @ write-string
    dup item-type@ save-fid @ write-line

    save-fid @ write-indent s" title: " save-fid @ write-string
    dup item-title@ save-fid @ write-line

    save-fid @ write-indent s" status: " save-fid @ write-string
    dup item-status@ save-fid @ write-line

    \ Priority (if set)
    dup item-priority@ nip 0> if
        save-fid @ write-indent s" priority: " save-fid @ write-string
        dup item-priority@ save-fid @ write-line
    then

    \ Tags (if any)
    dup item-tags-count@ 0> if
        save-fid @ write-indent s" tags: " save-fid @ write-string
        dup item-tags-count@ 0 ?do
            dup i item-tag@
            i 0> if s"  " save-fid @ write-string then
            save-fid @ write-string
        loop
        s" " save-fid @ write-line
    then

    \ Deps (if any)
    dup item-deps-count@ 0> if
        save-fid @ write-indent s" deps: " save-fid @ write-string
        dup item-deps-count@ 0 ?do
            dup i item-dep@
            i 0> if s"  " save-fid @ write-string then
            save-fid @ write-string
        loop
        s" " save-fid @ write-line
    then

    \ Assignee (if set)
    dup item-assignee@ nip 0> if
        save-fid @ write-indent s" assignee: " save-fid @ write-string
        dup item-assignee@ save-fid @ write-line
    then

    \ Created (if set)
    dup item-created@ nip 0> if
        save-fid @ write-indent s" created: " save-fid @ write-string
        dup item-created@ save-fid @ write-line
    then

    \ Desc (if set)
    dup item-desc@ nip 0> if
        save-fid @ write-indent s" desc: " save-fid @ write-string
        dup item-desc@ save-fid @ write-line
    then

    drop
    s" ]item" save-fid @ write-line
    s" " save-fid @ write-line ;

: save-data ( c-addr u -- flag )
    w/o create-file if drop false exit then
    save-fid !

    \ Write header
    s" # Kanban data file" save-fid @ write-line
    s" board: default" save-fid @ write-line
    s" lanes: " save-fid @ write-string
    lane-count @ 0 ?do
        i lane@
        i 0> if s"  " save-fid @ write-string then
        save-fid @ write-string
    loop
    s" " save-fid @ write-line
    s" " save-fid @ write-line

    \ Write items
    item-count @ 0 ?do
        i item-addr save-item
    loop

    save-fid @ close-file drop
    true ;

: save-default ( -- )
    default-data-file save-data drop ;

\ ============================================================
\ SECTION 8: CLI Commands
\ ============================================================

\ Date helper (simplified - just returns fixed date)
: today-date ( -- c-addr u )
    s" 2025-02-14" ;

\ --- kb add ---
: cmd-add ( type-addr type-len title-addr title-len -- )
    new-item dup 0= if drop 2drop 2drop exit then
    >r
    generate-id r@ item-id!
    r@ item-title!
    r@ item-type!
    s" backlog" r@ item-status!
    today-date r@ item-created!
    ." Added: " r> item-id@ type cr
    save-default ;

\ --- kb move ---
: cmd-move ( id-addr id-len lane-addr lane-len -- )
    2swap find-item-by-id
    dup 0= if
        drop 2drop
        ." Item not found" cr exit
    then
    >r
    \ Validate lane
    2dup lane-find -1 = if
        2drop r> drop
        ." Unknown lane" cr exit
    then
    r> item-status!
    ." Moved" cr
    save-default ;

\ --- kb show ---
variable show-item

: cmd-show ( id-addr id-len -- )
    find-item-by-id
    dup 0= if
        drop ." Item not found" cr exit
    then
    show-item !
    ." [" show-item @ item-id@ type ." ] "
    show-item @ item-type@ type ." : "
    show-item @ item-title@ type cr
    ."   Status: " show-item @ item-status@ type cr
    show-item @ item-priority@ nip 0> if
        ."   Priority: " show-item @ item-priority@ type cr
    then
    show-item @ item-tags-count@ 0> if
        ."   Tags: "
        show-item @ item-tags-count@ 0 ?do
            show-item @ i item-tag@ type
            i show-item @ item-tags-count@ 1- < if ." , " then
        loop cr
    then
    show-item @ item-deps-count@ 0> if
        ."   Deps: "
        show-item @ item-deps-count@ 0 ?do
            show-item @ i item-dep@ type
            i show-item @ item-deps-count@ 1- < if ." , " then
        loop cr
    then
    show-item @ item-assignee@ nip 0> if
        ."   Assignee: " show-item @ item-assignee@ type cr
    then
    show-item @ item-created@ nip 0> if
        ."   Created: " show-item @ item-created@ type cr
    then
    show-item @ item-desc@ nip 0> if
        ."   Desc: " show-item @ item-desc@ type cr
    then ;

\ --- kb ls ---
: cmd-ls-all ( -- )
    item-count @ 0= if
        ." No items" cr exit
    then
    item-count @ 0 ?do
        i item-addr >r
        ." [" r@ item-id@ type ." ] "
        r@ item-status@ type ."  | "
        r@ item-type@ type ." : "
        r@ item-title@ type cr
        r> drop
    loop ;

: cmd-ls-lane ( lane-addr lane-len -- )
    item-count @ 0 ?do
        i item-addr >r
        2dup r@ item-status@ str-equal? if
            ." [" r@ item-id@ type ." ] "
            r@ item-type@ type ." : "
            r@ item-title@ type cr
        then
        r> drop
    loop
    2drop ;

\ --- kb board ---
: board-separator ( width -- )
    0 ?do [char] - emit loop ;

: max-title-width ( -- n )
    20  \ minimum
    item-count @ 0 ?do
        i item-addr item-title@ nip max
    loop
    40 min ;  \ cap at 40

: print-padded ( c-addr u width -- )
    over - >r
    type
    r> 0 max spaces ;

variable col-width

: cmd-board ( -- )
    max-title-width 10 + col-width !

    \ Header
    lane-count @ 0 ?do
        i lane@ col-width @ print-padded
        i lane-count @ 1- < if ." | " then
    loop cr

    \ Separator
    lane-count @ 0 ?do
        col-width @ board-separator
        i lane-count @ 1- < if ." +" then
    loop cr

    \ Find max items in any lane
    0
    lane-count @ 0 ?do
        0
        item-count @ 0 ?do
            i item-addr item-status@ j lane@ str-equal? if 1+ then
        loop
        max
    loop

    \ Print rows
    \ Outer loop: row index (k in innermost)
    \ Middle loop: lane index (j in innermost)
    \ Inner loop: item index (i)
    dup 0 ?do
        lane-count @ 0 ?do
            \ Find k-th item in lane j
            0  \ counter for items in this lane
            item-count @ 0 ?do
                i item-addr item-status@ j lane@ str-equal? if
                    dup k = if
                        i item-addr >r
                        ." [" r@ item-id@ type ." ] "
                        r@ item-title@
                        dup col-width @ 10 - > if
                            drop col-width @ 10 -
                        then
                        type
                        col-width @ r@ item-title@ nip min 10 + col-width @ swap - spaces
                        r> drop
                    then
                    1+
                then
            loop
            drop
            j lane-count @ 1- < if ." | " then
        loop cr
    loop
    drop ;

\ ============================================================
\ SECTION 9: Blocked Status Extension
\ ============================================================

: item-done? ( item-addr -- flag )
    item-status@ s" done" str-equal? ;

: dep-done? ( dep-id-addr dep-id-len -- flag )
    find-item-by-id
    dup 0= if drop true exit then  \ unknown dep treated as done
    item-done? ;

: item-blocked? ( item-addr -- flag )
    dup item-deps-count@ 0= if drop false exit then
    dup item-deps-count@ 0 ?do
        dup i item-dep@ dep-done? 0= if
            drop true unloop exit
        then
    loop
    drop false ;

: item-blocking-deps ( item-addr -- )
    \ Print which deps are blocking this item
    dup item-deps-count@ 0 ?do
        dup i item-dep@ 2dup dep-done? 0= if
            ."     blocked by: " type cr
        else
            2drop
        then
    loop
    drop ;

: cmd-blocked ( -- )
    0  \ counter
    item-count @ 0 ?do
        i item-addr dup item-blocked? if
            >r
            ." [" r@ item-id@ type ." ] "
            r@ item-type@ type ." : "
            r@ item-title@ type cr
            r@ item-blocking-deps
            r> drop
            1+
        else
            drop
        then
    loop
    dup 0= if ." No blocked items" cr then
    drop ;

\ ============================================================
\ SECTION 10: Command Line Interface
\ ============================================================

: usage ( -- )
    ." Usage: kb <command> [args]" cr
    ." Commands:" cr
    ."   add <type> <title>  - Add new item" cr
    ."   move <id> <lane>    - Move item to lane" cr
    ."   show <id>           - Show item details" cr
    ."   ls [--lane=X]       - List items" cr
    ."   board               - Show board view" cr
    ."   blocked             - Show blocked items" cr ;

: init ( -- )
    lane-init
    item-init
    id-init
    0 parsing-item !
    default-lanes
    try-load-data ;

\ Parse command line arguments
\ In GForth, we use next-arg to get arguments

: arg-equal? ( c-addr u c-addr2 u2 -- flag )
    str-equal? ;

: get-quoted-arg ( -- c-addr u )
    \ Get a quoted argument from command line
    next-arg
    dup 0= if exit then
    \ Check if starts with quote
    over c@ [char] " = if
        1 /string  \ skip opening quote
        \ Check if ends with quote
        2dup + 1- c@ [char] " = if
            1-  \ remove closing quote
        then
    then ;

: main ( -- )
    init

    next-arg  \ skip program name or get first arg

    \ Get command
    next-arg
    dup 0= if 2drop usage bye then

    2dup s" add" arg-equal? if
        2drop
        next-arg            \ type
        dup 0= if 2drop ." Missing type" cr bye then
        get-quoted-arg      \ title
        dup 0= if 2drop 2drop ." Missing title" cr bye then
        cmd-add
        bye
    then

    2dup s" move" arg-equal? if
        2drop
        next-arg            \ id
        dup 0= if 2drop ." Missing id" cr bye then
        next-arg            \ lane
        dup 0= if 2drop 2drop ." Missing lane" cr bye then
        cmd-move
        bye
    then

    2dup s" show" arg-equal? if
        2drop
        next-arg            \ id
        dup 0= if 2drop ." Missing id" cr bye then
        cmd-show
        bye
    then

    2dup s" ls" arg-equal? if
        2drop
        next-arg
        dup 0> if
            \ Check for --lane=X
            2dup s" --lane=" str-prefix? if
                7 /string cmd-ls-lane
            else
                2drop cmd-ls-all
            then
        else
            2drop cmd-ls-all
        then
        bye
    then

    2dup s" board" arg-equal? if
        2drop cmd-board bye
    then

    2dup s" blocked" arg-equal? if
        2drop cmd-blocked bye
    then

    2dup s" help" arg-equal? if
        2drop usage bye
    then

    ." Unknown command: " type cr
    usage ;
